using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Text.Json;
using AethericGm.Core.Profiles;
using AethericGm.Core.Rules;
using AethericGm.Core.Rules.Packages;
using Microsoft.Data.Sqlite;
using Renci.SshNet;

namespace AethericGm.Infrastructure.Rules.Packages;

public sealed partial class GitRulesPackageInstaller(
    string connectionString,
    string packageRoot,
    ISshCredentialService credentials,
    TimeProvider? timeProvider = null) : IRulesPackageInstaller
{
    private static readonly string[] PackageFiles = ["manifest.json", "record-types.json", "records.json", "character-sheet.json", "catalog.json"];
    private readonly TimeProvider clock = timeProvider ?? TimeProvider.System;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(packageRoot);
        await using var connection = await OpenAsync(cancellationToken);
        var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS ssh_known_hosts (
              owner_subject_id TEXT NOT NULL, host TEXT NOT NULL, port INTEGER NOT NULL,
              algorithm TEXT NOT NULL, public_key TEXT NOT NULL, fingerprint TEXT NOT NULL, accepted_at TEXT NOT NULL,
              PRIMARY KEY(owner_subject_id, host, port, algorithm)
            );
            CREATE TABLE IF NOT EXISTS installed_rules_packages (
              id TEXT PRIMARY KEY, owner_subject_id TEXT NOT NULL, ruleset_id TEXT NOT NULL, ruleset_version TEXT NOT NULL,
              name TEXT NOT NULL, repository_url TEXT NOT NULL, resolved_commit TEXT NOT NULL, installed_at TEXT NOT NULL,
              package_path TEXT NOT NULL, UNIQUE(owner_subject_id, ruleset_id, ruleset_version)
            );
            CREATE INDEX IF NOT EXISTS ix_installed_rules_packages_owner ON installed_rules_packages(owner_subject_id);
            """;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<InstalledRulesPackage>> ListAsync(string ownerSubjectId, CancellationToken cancellationToken = default)
    {
        ownerSubjectId = RequireOwner(ownerSubjectId);
        await using var connection = await OpenAsync(cancellationToken);
        var command = connection.CreateCommand();
        command.CommandText = "SELECT id,owner_subject_id,ruleset_id,ruleset_version,name,repository_url,resolved_commit,installed_at,package_path FROM installed_rules_packages WHERE owner_subject_id=$owner ORDER BY name COLLATE NOCASE";
        command.Parameters.AddWithValue("$owner", ownerSubjectId);
        var result = new List<InstalledRulesPackage>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken)) result.Add(ReadPackage(reader));
        return result;
    }

    public async Task<IReadOnlyList<RulesetDescriptor>> ListRulesetsAsync(string ownerSubjectId, CancellationToken cancellationToken = default)
    {
        var packages = await ListAsync(ownerSubjectId, cancellationToken);
        return packages.Select(package => new FileRulesCatalog(package.PackagePath).Resolve(package.Ruleset)
                ?? throw new InvalidDataException($"Installed ruleset '{package.Ruleset}' is unavailable."))
            .OrderBy(ruleset => ruleset.Name, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    public async Task<GitPackageInstallResult> InstallFromGitAsync(string ownerSubjectId, GitPackageInstallRequest request, CancellationToken cancellationToken = default)
    {
        ownerSubjectId = RequireOwner(ownerSubjectId);
        var source = GitSshSource.Parse(request.RepositoryUrl);
        var revision = NormalizeRevision(request.Revision);
        var scanned = await ScanHostAsync(source, cancellationToken);
        var accepted = await GetKnownHostAsync(ownerSubjectId, source, scanned.Algorithm, cancellationToken);
        var changed = accepted is not null && !CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(accepted.PublicKey), Encoding.UTF8.GetBytes(scanned.PublicKey));
        if (accepted is null || changed)
        {
            if (request.AcceptedHostFingerprint is null)
                return new GitPackageInstallResult.HostKeyApprovalRequired(new SshHostKey(source.Host, source.Port, scanned.Algorithm, scanned.Fingerprint), changed);
            if (!string.Equals(request.AcceptedHostFingerprint, scanned.Fingerprint, StringComparison.Ordinal))
                return new GitPackageInstallResult.HostKeyApprovalRequired(new SshHostKey(source.Host, source.Port, scanned.Algorithm, scanned.Fingerprint), true);
            await AcceptHostAsync(ownerSubjectId, source, scanned, cancellationToken);
        }

        return request.CredentialId is { } credentialId
            ? await credentials.UsePrivateKeyAsync(ownerSubjectId, credentialId,
                (privateKey, ct) => AcquireAsync(ownerSubjectId, source, revision, scanned, privateKey, request.Passphrase, ct), cancellationToken)
            : await AcquireAsync(ownerSubjectId, source, revision, scanned, null, null, cancellationToken);
    }

    private async Task<GitPackageInstallResult> AcquireAsync(string owner, GitSshSource source, string revision, ScannedHostKey hostKey,
        string? privateKey, string? passphrase, CancellationToken ct)
    {
        var operationRoot = Path.Combine(Path.GetTempPath(), $"aetheric-gm-package-{Guid.NewGuid():N}");
        var repositoryPath = Path.Combine(operationRoot, "repository");
        var stagingPath = Path.Combine(operationRoot, "package");
        Directory.CreateDirectory(repositoryPath);
        Directory.CreateDirectory(stagingPath);
        string? keyPath = null;
        PassphraseServer? passphraseServer = null;
        try
        {
            var knownHostsPath = Path.Combine(operationRoot, "known_hosts");
            await File.WriteAllTextAsync(knownHostsPath, hostKey.KnownHostsLine + Environment.NewLine, ct);
            SetOwnerOnly(knownHostsPath);
            if (privateKey is not null)
            {
                ValidateKeyPassphrase(privateKey, passphrase);
                keyPath = Path.Combine(operationRoot, "identity");
                // OpenSSH rejects otherwise valid PEM/OpenSSH key files when their final newline was
                // removed by storage normalization. Restore it only in the temporary materialization.
                await File.WriteAllTextAsync(keyPath, privateKey.TrimEnd() + "\n", ct);
                SetOwnerOnly(keyPath);
                if (!string.IsNullOrEmpty(passphrase)) passphraseServer = await PassphraseServer.StartAsync(passphrase, operationRoot, ct);
            }

            var sshArguments = BuildSshArguments(source, hostKey.Algorithm, knownHostsPath, keyPath, passphraseServer is not null);
            await RunGitAsync(repositoryPath, ["init", "--quiet"], null, ct);
            await RunGitAsync(repositoryPath, ["remote", "add", "origin", source.NormalizedUrl], null, ct);
            await RunGitAsync(repositoryPath, ["-c", $"core.sshCommand={sshArguments}", "fetch", "--quiet", "--depth=1", "--no-tags", "origin", revision], passphraseServer, ct);
            var commit = (await RunGitAsync(repositoryPath, ["rev-parse", "--verify", "FETCH_HEAD^{commit}"], null, ct)).Trim();
            if (!CommitRegex().IsMatch(commit)) throw new RulesPackageInstallException("Git did not resolve the requested revision to a commit.");

            var tree = await RunGitAsync(repositoryPath, ["ls-tree", "-r", "--name-only", commit], null, ct);
            var paths = tree.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (paths.Any(path => path.StartsWith('/') || path.Contains("..", StringComparison.Ordinal) || path.Contains('\\')))
                throw new RulesPackageInstallException("The package contains an unsafe path.");
            foreach (var file in PackageFiles.Where(paths.Contains))
            {
                var content = await RunGitAsync(repositoryPath, ["show", $"{commit}:{file}"], null, ct, 2 * 1024 * 1024);
                await File.WriteAllTextAsync(Path.Combine(stagingPath, file), content, ct);
            }
            if (!File.Exists(Path.Combine(stagingPath, "manifest.json"))) throw new RulesPackageInstallException("The selected revision does not contain a root manifest.json.");

            RulesetDescriptor descriptor;
            try
            {
                var catalog = new FileRulesCatalog(stagingPath);
                descriptor = catalog.List().Single();
                await ValidateCharacterSheetAsync(stagingPath, descriptor, ct);
            }
            catch (Exception exception) when (exception is InvalidDataException or JsonException or InvalidOperationException)
            { throw new RulesPackageInstallException($"Rules package validation failed: {exception.Message}"); }

            var existing = await FindAsync(owner, descriptor.Reference, ct);
            if (existing is not null && !string.Equals(existing.ResolvedCommit, commit, StringComparison.Ordinal))
                throw new RulesPackageInstallException($"Ruleset '{descriptor.Reference}' is already installed from a different commit. Use the explicit update flow to replace it.");
            if (existing is not null) return new GitPackageInstallResult.Installed(existing);

            var package = new InstalledRulesPackage(Guid.NewGuid(), owner, descriptor.Reference, descriptor.Name, source.NormalizedUrl, commit,
                clock.GetUtcNow(), Path.Combine(packageRoot, Guid.NewGuid().ToString("N")));
            Directory.Move(stagingPath, package.PackagePath);
            try { await InsertAsync(package, ct); }
            catch { Directory.Delete(package.PackagePath, true); throw; }
            return new GitPackageInstallResult.Installed(package);
        }
        catch (RulesPackageInstallException) { throw; }
        catch (OperationCanceledException) { throw; }
        catch { throw new RulesPackageInstallException("The repository could not be acquired. Check its address, revision, credential, and passphrase."); }
        finally
        {
            if (passphraseServer is not null) await passphraseServer.DisposeAsync();
            if (keyPath is not null && File.Exists(keyPath)) File.Delete(keyPath);
            if (Directory.Exists(operationRoot)) Directory.Delete(operationRoot, true);
        }
    }

    private static async Task ValidateCharacterSheetAsync(string root, RulesetDescriptor descriptor, CancellationToken ct)
    {
        var source = Path.Combine(root, "character-sheet.json");
        if (!File.Exists(source)) return;
        var expected = Path.Combine(root, descriptor.Reference.Id, descriptor.Reference.Version);
        Directory.CreateDirectory(expected);
        var target = Path.Combine(expected, "character-sheet.json");
        File.Move(source, target);
        try { _ = await new CharacterSheets.FileCharacterSheetDefinitionStore(root, new FileRulesCatalog(root)).GetAsync(descriptor.Reference, ct); }
        finally { File.Move(target, source); Directory.Delete(Path.Combine(root, descriptor.Reference.Id), true); }
    }

    private static void ValidateKeyPassphrase(string privateKey, string? passphrase)
    {
        try
        {
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(privateKey));
            using var key = string.IsNullOrEmpty(passphrase) ? new PrivateKeyFile(stream) : new PrivateKeyFile(stream, passphrase);
        }
        catch { throw new RulesPackageInstallException("The SSH credential passphrase is missing or incorrect."); }
    }

    private static string BuildSshArguments(GitSshSource source, string hostKeyAlgorithm, string knownHosts, string? keyPath, bool allowAskPass)
    {
        var arguments = $"ssh -o BatchMode={(allowAskPass ? "no" : "yes")} -o PasswordAuthentication=no -o KbdInteractiveAuthentication=no -o IdentitiesOnly=yes -o StrictHostKeyChecking=yes -o HostKeyAlgorithms={hostKeyAlgorithm} -o UserKnownHostsFile={Quote(knownHosts)} -p {source.Port}";
        if (keyPath is not null) arguments += $" -i {Quote(keyPath)}";
        return arguments;
    }

    private static async Task<string> RunGitAsync(string workingDirectory, IReadOnlyList<string> arguments, PassphraseServer? passphrase,
        CancellationToken ct, int outputLimit = 256 * 1024)
    {
        var start = new ProcessStartInfo("git") { WorkingDirectory = workingDirectory, RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
        foreach (var argument in arguments) start.ArgumentList.Add(argument);
        start.Environment["GIT_CONFIG_NOSYSTEM"] = "1";
        start.Environment["GIT_TERMINAL_PROMPT"] = "0";
        start.Environment.Remove("SSH_AUTH_SOCK");
        if (passphrase is not null)
        {
            start.Environment["SSH_ASKPASS"] = passphrase.ScriptPath;
            start.Environment["SSH_ASKPASS_REQUIRE"] = "force";
            start.Environment["DISPLAY"] = "aetheric-gm";
            start.Environment["AETHERIC_GM_ASKPASS_URL"] = passphrase.Url;
        }
        using var process = Process.Start(start) ?? throw new RulesPackageInstallException("Git could not be started.");
        var stdoutTask = process.StandardOutput.ReadToEndAsync(ct);
        var stderrTask = process.StandardError.ReadToEndAsync(ct);
        await process.WaitForExitAsync(ct);
        var stdout = await stdoutTask;
        var stderr = await stderrTask;
        if (stdout.Length > outputLimit) throw new RulesPackageInstallException("Repository package content exceeds the supported size limit.");
        if (process.ExitCode != 0) throw GitFailure(stderr);
        return stdout;
    }

    private static RulesPackageInstallException GitFailure(string diagnostic)
    {
        if (diagnostic.Contains("couldn't find remote ref", StringComparison.OrdinalIgnoreCase))
            return new("The requested branch, tag, or commit was not found.");
        if (diagnostic.Contains("Repository not found", StringComparison.OrdinalIgnoreCase))
            return new("The repository was not found, or the selected credential cannot access it.");
        if (diagnostic.Contains("Permission denied (publickey", StringComparison.OrdinalIgnoreCase))
            return new("GitHub rejected the selected SSH credential. Confirm that its public key has access to this repository.");
        if (diagnostic.Contains("incorrect passphrase", StringComparison.OrdinalIgnoreCase) || diagnostic.Contains("error in libcrypto", StringComparison.OrdinalIgnoreCase))
            return new("OpenSSH could not unlock the selected private key. Check its passphrase.");
        if (diagnostic.Contains("Host key verification failed", StringComparison.OrdinalIgnoreCase))
            return new("SSH host verification failed; review the host identity again.");
        if (diagnostic.Contains("Could not resolve hostname", StringComparison.OrdinalIgnoreCase))
            return new("The SSH repository host could not be resolved.");
        return new("Git could not acquire the requested repository revision.");
    }

    private static async Task<ScannedHostKey> ScanHostAsync(GitSshSource source, CancellationToken ct)
    {
        var start = new ProcessStartInfo("ssh-keyscan") { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
        start.ArgumentList.Add("-T"); start.ArgumentList.Add("10"); start.ArgumentList.Add("-p"); start.ArgumentList.Add(source.Port.ToString());
        start.ArgumentList.Add("-t"); start.ArgumentList.Add("ed25519,ecdsa,rsa"); start.ArgumentList.Add(source.Host);
        using var process = Process.Start(start) ?? throw new RulesPackageInstallException("SSH host inspection could not be started.");
        var outputTask = process.StandardOutput.ReadToEndAsync(ct);
        await process.WaitForExitAsync(ct);
        var line = (await outputTask).Split('\n', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault(value => !value.StartsWith('#'));
        if (process.ExitCode != 0 || line is null) throw new RulesPackageInstallException("The SSH host could not be reached for identity verification.");
        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 3) throw new RulesPackageInstallException("The SSH host returned an invalid identity.");
        byte[] key;
        try { key = Convert.FromBase64String(parts[2]); }
        catch (FormatException) { throw new RulesPackageInstallException("The SSH host returned an invalid identity."); }
        var fingerprint = Convert.ToBase64String(SHA256.HashData(key)).TrimEnd('=');
        return new ScannedHostKey(parts[1], parts[2], $"SHA256:{fingerprint}", line);
    }

    private async Task<KnownHost?> GetKnownHostAsync(string owner, GitSshSource source, string algorithm, CancellationToken ct)
    {
        await using var connection = await OpenAsync(ct); var command = connection.CreateCommand();
        command.CommandText = "SELECT public_key,fingerprint FROM ssh_known_hosts WHERE owner_subject_id=$owner AND host=$host AND port=$port AND algorithm=$algorithm";
        command.Parameters.AddWithValue("$owner", owner); command.Parameters.AddWithValue("$host", source.Host); command.Parameters.AddWithValue("$port", source.Port); command.Parameters.AddWithValue("$algorithm", algorithm);
        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? new KnownHost(reader.GetString(0), reader.GetString(1)) : null;
    }

    private async Task AcceptHostAsync(string owner, GitSshSource source, ScannedHostKey key, CancellationToken ct)
    {
        await using var connection = await OpenAsync(ct); var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO ssh_known_hosts(owner_subject_id,host,port,algorithm,public_key,fingerprint,accepted_at) VALUES($owner,$host,$port,$algorithm,$key,$fingerprint,$at) ON CONFLICT(owner_subject_id,host,port,algorithm) DO UPDATE SET public_key=$key,fingerprint=$fingerprint,accepted_at=$at";
        command.Parameters.AddWithValue("$owner", owner); command.Parameters.AddWithValue("$host", source.Host); command.Parameters.AddWithValue("$port", source.Port); command.Parameters.AddWithValue("$algorithm", key.Algorithm); command.Parameters.AddWithValue("$key", key.PublicKey); command.Parameters.AddWithValue("$fingerprint", key.Fingerprint); command.Parameters.AddWithValue("$at", clock.GetUtcNow().ToString("O"));
        await command.ExecuteNonQueryAsync(ct);
    }

    private async Task<InstalledRulesPackage?> FindAsync(string owner, RulesetReference ruleset, CancellationToken ct)
    {
        await using var connection = await OpenAsync(ct); var command = connection.CreateCommand();
        command.CommandText = "SELECT id,owner_subject_id,ruleset_id,ruleset_version,name,repository_url,resolved_commit,installed_at,package_path FROM installed_rules_packages WHERE owner_subject_id=$owner AND ruleset_id=$id AND ruleset_version=$version";
        command.Parameters.AddWithValue("$owner", owner); command.Parameters.AddWithValue("$id", ruleset.Id); command.Parameters.AddWithValue("$version", ruleset.Version);
        await using var reader = await command.ExecuteReaderAsync(ct); return await reader.ReadAsync(ct) ? ReadPackage(reader) : null;
    }

    private async Task InsertAsync(InstalledRulesPackage package, CancellationToken ct)
    {
        await using var connection = await OpenAsync(ct); var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO installed_rules_packages(id,owner_subject_id,ruleset_id,ruleset_version,name,repository_url,resolved_commit,installed_at,package_path) VALUES($id,$owner,$ruleset,$version,$name,$url,$commit,$at,$path)";
        command.Parameters.AddWithValue("$id", package.Id.ToString()); command.Parameters.AddWithValue("$owner", package.OwnerSubjectId); command.Parameters.AddWithValue("$ruleset", package.Ruleset.Id); command.Parameters.AddWithValue("$version", package.Ruleset.Version); command.Parameters.AddWithValue("$name", package.Name); command.Parameters.AddWithValue("$url", package.RepositoryUrl); command.Parameters.AddWithValue("$commit", package.ResolvedCommit); command.Parameters.AddWithValue("$at", package.InstalledAt.ToString("O")); command.Parameters.AddWithValue("$path", package.PackagePath);
        await command.ExecuteNonQueryAsync(ct);
    }

    private async Task<SqliteConnection> OpenAsync(CancellationToken ct) { var connection = new SqliteConnection(connectionString); await connection.OpenAsync(ct); return connection; }
    private static InstalledRulesPackage ReadPackage(SqliteDataReader reader) => new(Guid.Parse(reader.GetString(0)), reader.GetString(1), new RulesetReference(reader.GetString(2), reader.GetString(3)), reader.GetString(4), reader.GetString(5), reader.GetString(6), DateTimeOffset.Parse(reader.GetString(7)), reader.GetString(8));
    private static string NormalizeRevision(string revision)
    {
        var normalized = revision?.Trim() ?? "";
        return RevisionRegex().IsMatch(normalized) ? normalized : throw new RulesPackageInstallException("Revision must be a branch, tag, or commit name without spaces.");
    }
    private static string RequireOwner(string owner) => string.IsNullOrWhiteSpace(owner) ? throw new ArgumentException("Owner subject ID is required.", nameof(owner)) : owner.Trim();
    private static void SetOwnerOnly(string path) { if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite); }
    private static string Quote(string value) => $"'{value.Replace("'", "'\\''", StringComparison.Ordinal)}'";
    [GeneratedRegex("^[A-Za-z0-9][A-Za-z0-9._/-]{0,199}$", RegexOptions.CultureInvariant)] private static partial Regex RevisionRegex();
    [GeneratedRegex("^[0-9a-f]{40,64}$", RegexOptions.CultureInvariant)] private static partial Regex CommitRegex();

    private sealed record KnownHost(string PublicKey, string Fingerprint);
    private sealed record ScannedHostKey(string Algorithm, string PublicKey, string Fingerprint, string KnownHostsLine);

    private sealed partial record GitSshSource(string Host, int Port, string NormalizedUrl)
    {
        public static GitSshSource Parse(string value)
        {
            value = value?.Trim() ?? "";
            if (Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme == "ssh" && !string.IsNullOrWhiteSpace(uri.Host) && !string.IsNullOrWhiteSpace(uri.AbsolutePath))
            {
                if (!string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment) || !string.IsNullOrEmpty(uri.UserInfo) && uri.UserInfo.Contains(':')) throw Invalid();
                var user = string.IsNullOrWhiteSpace(uri.UserInfo) ? "git" : uri.UserInfo;
                var port = uri.IsDefaultPort ? 22 : uri.Port;
                return new(uri.IdnHost.ToLowerInvariant(), port, $"ssh://{user}@{uri.IdnHost.ToLowerInvariant()}:{port}{uri.AbsolutePath}");
            }
            var match = ScpRegex().Match(value);
            if (!match.Success) throw Invalid();
            var host = match.Groups["host"].Value.ToLowerInvariant();
            return new(host, 22, $"{match.Groups["user"].Value}@{host}:{match.Groups["path"].Value}");
        }
        private static RulesPackageInstallException Invalid() => new("Repository address must be an SSH URL such as git@host:owner/repository.git.");
        [GeneratedRegex("^(?<user>[A-Za-z0-9._-]+)@(?<host>[A-Za-z0-9.-]+):(?<path>[^\\s?#]+)$", RegexOptions.CultureInvariant)] private static partial Regex ScpRegex();
    }

    private sealed class PassphraseServer : IAsyncDisposable
    {
        private readonly HttpListener listener;
        private readonly CancellationTokenSource stop = new();
        private readonly Task serveTask;
        private string passphrase;
        public string ScriptPath { get; }
        public string Url { get; }
        private PassphraseServer(HttpListener listener, string passphrase, string scriptPath, string url)
        { this.listener = listener; this.passphrase = passphrase; ScriptPath = scriptPath; Url = url; serveTask = ServeAsync(); }
        public static Task<PassphraseServer> StartAsync(string passphrase, string directory, CancellationToken ct)
        {
            var port = RandomNumberGenerator.GetInt32(20000, 60000); var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(24));
            var url = $"http://127.0.0.1:{port}/{token}/"; var listener = new HttpListener(); listener.Prefixes.Add(url); listener.Start();
            var script = Path.Combine(directory, "askpass"); File.WriteAllText(script, "#!/bin/sh\nexec curl --silent --show-error --fail --max-time 5 \"$AETHERIC_GM_ASKPASS_URL\"\n");
            if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(script, UnixFileMode.UserRead | UnixFileMode.UserExecute);
            return Task.FromResult(new PassphraseServer(listener, passphrase, script, url));
        }
        private async Task ServeAsync()
        {
            try { while (!stop.IsCancellationRequested) { var context = await listener.GetContextAsync().WaitAsync(stop.Token); var bytes = Encoding.UTF8.GetBytes(passphrase); context.Response.ContentLength64 = bytes.Length; await context.Response.OutputStream.WriteAsync(bytes); context.Response.Close(); } }
            catch (OperationCanceledException) { }
            catch (HttpListenerException) when (stop.IsCancellationRequested) { }
        }
        public async ValueTask DisposeAsync() { passphrase = string.Empty; stop.Cancel(); listener.Close(); try { await serveTask; } catch { } stop.Dispose(); }
    }
}
