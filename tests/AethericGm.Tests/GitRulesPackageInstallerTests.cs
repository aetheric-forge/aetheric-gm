using AethericGm.Core.Profiles;
using AethericGm.Core.Rules.Packages;
using AethericGm.Infrastructure.Rules.Packages;

namespace AethericGm.Tests;

public sealed class GitRulesPackageInstallerTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), $"aetheric-packages-{Guid.NewGuid():N}");
    private string ConnectionString => $"Data Source={Path.Combine(root, "packages.db")}";

    [Fact]
    public async Task Initializes_an_owner_scoped_empty_package_catalog()
    {
        Directory.CreateDirectory(root);
        var installer = new GitRulesPackageInstaller(ConnectionString, Path.Combine(root, "cache"), new UnusedCredentials());
        await installer.InitializeAsync();

        Assert.Empty(await installer.ListAsync("operator-a"));
        Assert.Empty(await installer.ListRulesetsAsync("operator-b"));
    }

    [Theory]
    [InlineData("https://example.com/rules.git", "main")]
    [InlineData("ssh://user:secret@example.com/rules.git", "main")]
    [InlineData("git@example.com:rules.git", "--upload-pack=evil")]
    public async Task Rejects_non_ssh_secret_bearing_or_option_like_sources_before_acquisition(string repository, string revision)
    {
        Directory.CreateDirectory(root);
        var installer = new GitRulesPackageInstaller(ConnectionString, Path.Combine(root, "cache"), new UnusedCredentials());
        await installer.InitializeAsync();

        await Assert.ThrowsAsync<RulesPackageInstallException>(() => installer.InstallFromGitAsync("operator", new(repository, revision, null, null)));
    }

    public void Dispose() { if (Directory.Exists(root)) Directory.Delete(root, true); }

    private sealed class UnusedCredentials : ISshCredentialService
    {
        public Task InitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<IReadOnlyList<SshCredential>> ListAsync(string ownerSubjectId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<SshCredential>>([]);
        public Task<SshCredential?> GetAsync(string ownerSubjectId, Guid id, CancellationToken cancellationToken = default) => Task.FromResult<SshCredential?>(null);
        public Task<SshCredential> AddAsync(string ownerSubjectId, string name, string privateKey, string? passphrase, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<T> UsePrivateKeyAsync<T>(string ownerSubjectId, Guid id, Func<string, CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task RenameAsync(string ownerSubjectId, Guid id, string name, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DeleteAsync(string ownerSubjectId, Guid id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
