namespace AethericGm.Core.Rules.Packages;

public interface IRulesPackageInstaller
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InstalledRulesPackage>> ListAsync(string ownerSubjectId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RulesetDescriptor>> ListRulesetsAsync(string ownerSubjectId, CancellationToken cancellationToken = default);
    Task<GitPackageInstallResult> InstallFromGitAsync(string ownerSubjectId, GitPackageInstallRequest request, CancellationToken cancellationToken = default);
}

public sealed record GitPackageInstallRequest(string RepositoryUrl, string Revision, Guid? CredentialId, string? Passphrase, string? AcceptedHostFingerprint = null);

public abstract record GitPackageInstallResult
{
    private GitPackageInstallResult() { }
    public sealed record HostKeyApprovalRequired(SshHostKey HostKey, bool Changed) : GitPackageInstallResult;
    public sealed record Installed(InstalledRulesPackage Package) : GitPackageInstallResult;
}

public sealed record SshHostKey(string Host, int Port, string Algorithm, string Fingerprint);

public sealed record InstalledRulesPackage(Guid Id, string OwnerSubjectId, RulesetReference Ruleset, string Name,
    string RepositoryUrl, string ResolvedCommit, DateTimeOffset InstalledAt, string PackagePath);

public sealed class RulesPackageInstallException(string message) : Exception(message);
