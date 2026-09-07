using System.Security.Cryptography;
using AethericGm.Core.Profiles;
using AethericGm.Infrastructure.Profiles;
using AethericGm.Web.Profiles;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Data.Sqlite;

namespace AethericGm.Tests;

public sealed class SshCredentialTests : IDisposable
{
    private readonly string databasePath = Path.Combine(Path.GetTempPath(), $"aetheric-credentials-{Guid.NewGuid():N}.db");
    public void Dispose() { if (File.Exists(databasePath)) File.Delete(databasePath); }

    [Fact]
    public async Task Stores_encrypted_metadata_and_enforces_profile_ownership()
    {
        var service = CreateService();
        await service.InitializeAsync();
        var privateKey = CreatePrivateKey();

        var added = await service.AddAsync("operator-a", "Rules repository", privateKey, null);

        Assert.StartsWith("ssh-rsa", added.Algorithm);
        Assert.StartsWith("SHA256:", added.Fingerprint);
        Assert.False(added.RequiresPassphrase);
        Assert.Single(await service.ListAsync("operator-a"));
        Assert.Empty(await service.ListAsync("operator-b"));
        Assert.Null(await service.GetAsync("operator-b", added.Id));

        await using var connection = new SqliteConnection($"Data Source={databasePath}");
        await connection.OpenAsync();
        var command = connection.CreateCommand(); command.CommandText = "SELECT protected_private_key FROM ssh_credentials WHERE id=$id"; command.Parameters.AddWithValue("$id", added.Id.ToString());
        var stored = Assert.IsType<string>(await command.ExecuteScalarAsync());
        Assert.DoesNotContain("PRIVATE KEY", stored, StringComparison.Ordinal);
        Assert.NotEqual(privateKey, stored);
    }

    [Fact]
    public async Task Validates_passphrase_protected_keys_without_storing_the_passphrase()
    {
        const string passphrase = "correct horse battery staple";
        var service = CreateService(); await service.InitializeAsync();
        var privateKey = CreateEncryptedPrivateKey(passphrase);

        await Assert.ThrowsAsync<SshCredentialValidationException>(() => service.AddAsync("operator", "Encrypted", privateKey, null));
        var failure = await Assert.ThrowsAsync<SshCredentialValidationException>(() => service.AddAsync("operator", "Encrypted", privateKey, "wrong"));
        Assert.DoesNotContain("wrong", failure.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("PRIVATE KEY", failure.Message, StringComparison.Ordinal);
        var added = await service.AddAsync("operator", "Encrypted", privateKey, passphrase);

        Assert.True(added.RequiresPassphrase);
        await using var connection = new SqliteConnection($"Data Source={databasePath}"); await connection.OpenAsync();
        var command = connection.CreateCommand(); command.CommandText = "SELECT protected_private_key FROM ssh_credentials WHERE id=$id"; command.Parameters.AddWithValue("$id", added.Id.ToString());
        Assert.DoesNotContain(passphrase, Assert.IsType<string>(await command.ExecuteScalarAsync()), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Detects_encrypted_openssh_ed25519_keys_and_preserves_metadata()
    {
        const string passphrase = "test-only-passphrase";
        var directory = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $"gm-ed25519-{Guid.NewGuid():N}"));
        try
        {
            var path = Path.Combine(directory.FullName, "key");
            var start = new System.Diagnostics.ProcessStartInfo("ssh-keygen") { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
            foreach (var argument in new[] { "-q", "-t", "ed25519", "-N", passphrase, "-f", path }) start.ArgumentList.Add(argument);
            using var process = System.Diagnostics.Process.Start(start)!;
            await process.WaitForExitAsync();
            Assert.Equal(0, process.ExitCode);
            var key = await File.ReadAllTextAsync(path);
            var service = CreateService();
            await service.InitializeAsync();
            await Assert.ThrowsAsync<SshCredentialValidationException>(() => service.AddAsync("owner", "Encrypted", key, null));
            await Assert.ThrowsAsync<SshCredentialValidationException>(() => service.AddAsync("owner", "Encrypted", key, "wrong"));
            var added = await service.AddAsync("owner", "Encrypted", key, passphrase);
            Assert.True(added.RequiresPassphrase);
            Assert.True(Assert.Single(await service.ListAsync("owner")).RequiresPassphrase);
            Assert.True((await service.GetAsync("owner", added.Id))!.RequiresPassphrase);
        }
        finally { directory.Delete(true); }
    }

    [Fact]
    public async Task Does_not_silently_ignore_a_supplied_passphrase_when_key_opens_without_it()
    {
        var service = CreateService();
        await service.InitializeAsync();
        var error = await Assert.ThrowsAsync<SshCredentialValidationException>(() =>
            service.AddAsync("owner", "Unexpected key", CreatePrivateKey(), "arbitrary-input"));
        Assert.Contains("opened without a passphrase", error.Message);
        Assert.DoesNotContain("arbitrary-input", error.Message);
        Assert.Empty(await service.ListAsync("owner"));
    }

    [Fact]
    public async Task Renames_and_removes_only_credentials_owned_by_the_profile()
    {
        var service = CreateService(); await service.InitializeAsync();
        var added = await service.AddAsync("owner", "Original", CreatePrivateKey(), null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.RenameAsync("other", added.Id, "Stolen"));
        await service.RenameAsync("owner", added.Id, "Renamed");
        Assert.Equal("Renamed", (await service.GetAsync("owner", added.Id))!.Name);
        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.DeleteAsync("other", added.Id));
        await service.DeleteAsync("owner", added.Id);
        Assert.Empty(await service.ListAsync("owner"));
    }

    [Fact]
    public async Task Decrypts_private_material_only_inside_an_owner_authorized_operation()
    {
        var service = CreateService(); await service.InitializeAsync();
        var privateKey = CreatePrivateKey();
        var added = await service.AddAsync("owner", "Repository", privateKey, null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.UsePrivateKeyAsync("other", added.Id, (key, _) => Task.FromResult(key.Length)));
        var observed = await service.UsePrivateKeyAsync("owner", added.Id, (key, _) => Task.FromResult(key));

        Assert.Equal(privateKey.Trim(), observed);
        Assert.NotNull((await service.GetAsync("owner", added.Id))!.LastUsedAt);
    }

    [Fact]
    public void Data_protected_key_cannot_be_opened_with_different_application_keys()
    {
        var firstPath = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $"aetheric-protection-{Guid.NewGuid():N}"));
        var secondPath = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $"aetheric-protection-{Guid.NewGuid():N}"));
        try
        {
            var first = new DataProtectionSshPrivateKeyProtector(DataProtectionProvider.Create(firstPath));
            var second = new DataProtectionSshPrivateKeyProtector(DataProtectionProvider.Create(secondPath));
            var protectedKey = first.Protect(CreatePrivateKey());
            Assert.Throws<CryptographicException>(() => second.Unprotect(protectedKey));
        }
        finally { Directory.Delete(firstPath.FullName, true); Directory.Delete(secondPath.FullName, true); }
    }

    private SqliteSshCredentialService CreateService() => new($"Data Source={databasePath}", new TestProtector());
    private static string CreatePrivateKey() { using var rsa = RSA.Create(2048); return rsa.ExportPkcs8PrivateKeyPem(); }
    private static string CreateEncryptedPrivateKey(string passphrase)
    {
        using var rsa = RSA.Create(2048);
        return rsa.ExportEncryptedPkcs8PrivateKeyPem(passphrase, new PbeParameters(PbeEncryptionAlgorithm.Aes256Cbc, HashAlgorithmName.SHA256, 10_000));
    }

    private sealed class TestProtector : ISshPrivateKeyProtector
    {
        public string Protect(string privateKey) => Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(privateKey));
        public string Unprotect(string protectedPrivateKey) => System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(protectedPrivateKey));
    }
}
