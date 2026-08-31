namespace AethericGm.Core.Profiles;

public interface ISshCredentialService
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SshCredential>> ListAsync(string ownerSubjectId, CancellationToken cancellationToken = default);
    Task<SshCredential?> GetAsync(string ownerSubjectId, Guid id, CancellationToken cancellationToken = default);
    Task<SshCredential> AddAsync(string ownerSubjectId, string name, string privateKey, string? passphrase, CancellationToken cancellationToken = default);
    Task<T> UsePrivateKeyAsync<T>(string ownerSubjectId, Guid id, Func<string, CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default);
    Task RenameAsync(string ownerSubjectId, Guid id, string name, CancellationToken cancellationToken = default);
    Task DeleteAsync(string ownerSubjectId, Guid id, CancellationToken cancellationToken = default);
}

public interface ISshPrivateKeyProtector
{
    string Protect(string privateKey);
    string Unprotect(string protectedPrivateKey);
}

public sealed class SshCredentialValidationException(string message) : Exception(message);
