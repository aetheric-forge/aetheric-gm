using AethericGm.Core.Profiles;
using Microsoft.Data.Sqlite;

namespace AethericGm.Infrastructure.Profiles;

public sealed class SqliteSshCredentialService(string connectionString, ISshPrivateKeyProtector protector, TimeProvider? timeProvider = null) : ISshCredentialService
{
    private readonly TimeProvider clock = timeProvider ?? TimeProvider.System;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken);
        var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS ssh_credentials (
              id TEXT PRIMARY KEY,
              owner_subject_id TEXT NOT NULL,
              name TEXT NOT NULL COLLATE NOCASE,
              algorithm TEXT NOT NULL,
              fingerprint TEXT NOT NULL,
              requires_passphrase INTEGER NOT NULL,
              protected_private_key TEXT NOT NULL,
              created_at TEXT NOT NULL,
              last_used_at TEXT NULL,
              UNIQUE(owner_subject_id, name)
            );
            CREATE INDEX IF NOT EXISTS ix_ssh_credentials_owner ON ssh_credentials(owner_subject_id);
            """;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SshCredential>> ListAsync(string ownerSubjectId, CancellationToken cancellationToken = default)
    {
        ownerSubjectId = RequireOwner(ownerSubjectId);
        await using var connection = await OpenAsync(cancellationToken);
        var command = connection.CreateCommand();
        command.CommandText = "SELECT id,owner_subject_id,name,algorithm,fingerprint,requires_passphrase,created_at,last_used_at FROM ssh_credentials WHERE owner_subject_id=$owner ORDER BY name COLLATE NOCASE";
        command.Parameters.AddWithValue("$owner", ownerSubjectId);
        var result = new List<SshCredential>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken)) result.Add(Read(reader));
        return result;
    }

    public async Task<SshCredential?> GetAsync(string ownerSubjectId, Guid id, CancellationToken cancellationToken = default)
    {
        ownerSubjectId = RequireOwner(ownerSubjectId);
        await using var connection = await OpenAsync(cancellationToken);
        var command = connection.CreateCommand();
        command.CommandText = "SELECT id,owner_subject_id,name,algorithm,fingerprint,requires_passphrase,created_at,last_used_at FROM ssh_credentials WHERE owner_subject_id=$owner AND id=$id";
        command.Parameters.AddWithValue("$owner", ownerSubjectId);
        command.Parameters.AddWithValue("$id", id.ToString());
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? Read(reader) : null;
    }

    public async Task<SshCredential> AddAsync(string ownerSubjectId, string name, string privateKey, string? passphrase, CancellationToken cancellationToken = default)
    {
        ownerSubjectId = RequireOwner(ownerSubjectId);
        var inspection = SshPrivateKeyInspector.Inspect(privateKey, passphrase);
        var credential = new SshCredential(Guid.NewGuid(), ownerSubjectId, name, inspection.Algorithm, inspection.Fingerprint, inspection.RequiresPassphrase, clock.GetUtcNow());
        var protectedPrivateKey = protector.Protect(privateKey.Trim());

        await using var connection = await OpenAsync(cancellationToken);
        var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO ssh_credentials(id,owner_subject_id,name,algorithm,fingerprint,requires_passphrase,protected_private_key,created_at,last_used_at) VALUES($id,$owner,$name,$algorithm,$fingerprint,$passphrase,$key,$created,NULL)";
        AddParameters(command, credential);
        command.Parameters.AddWithValue("$key", protectedPrivateKey);
        try { await command.ExecuteNonQueryAsync(cancellationToken); }
        catch (SqliteException exception) when (exception.SqliteErrorCode == 19) { throw new InvalidOperationException($"An SSH credential named '{credential.Name}' already exists."); }
        return credential;
    }

    public async Task<T> UsePrivateKeyAsync<T>(string ownerSubjectId, Guid id, Func<string, CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default)
    {
        ownerSubjectId = RequireOwner(ownerSubjectId);
        ArgumentNullException.ThrowIfNull(operation);
        await using var connection = await OpenAsync(cancellationToken);
        var command = connection.CreateCommand();
        command.CommandText = "SELECT protected_private_key FROM ssh_credentials WHERE owner_subject_id=$owner AND id=$id";
        command.Parameters.AddWithValue("$owner", ownerSubjectId);
        command.Parameters.AddWithValue("$id", id.ToString());
        var protectedKey = await command.ExecuteScalarAsync(cancellationToken) as string ?? throw new KeyNotFoundException("SSH credential was not found.");
        var privateKey = protector.Unprotect(protectedKey);
        try
        {
            var result = await operation(privateKey, cancellationToken);
            var update = connection.CreateCommand();
            update.CommandText = "UPDATE ssh_credentials SET last_used_at=$used WHERE owner_subject_id=$owner AND id=$id";
            update.Parameters.AddWithValue("$used", clock.GetUtcNow().ToString("O"));
            update.Parameters.AddWithValue("$owner", ownerSubjectId);
            update.Parameters.AddWithValue("$id", id.ToString());
            await update.ExecuteNonQueryAsync(cancellationToken);
            return result;
        }
        finally { privateKey = string.Empty; }
    }

    public async Task RenameAsync(string ownerSubjectId, Guid id, string name, CancellationToken cancellationToken = default)
    {
        ownerSubjectId = RequireOwner(ownerSubjectId);
        var normalized = SshCredential.NormalizeName(name);
        await using var connection = await OpenAsync(cancellationToken);
        var command = connection.CreateCommand();
        command.CommandText = "UPDATE ssh_credentials SET name=$name WHERE owner_subject_id=$owner AND id=$id";
        command.Parameters.AddWithValue("$name", normalized);
        command.Parameters.AddWithValue("$owner", ownerSubjectId);
        command.Parameters.AddWithValue("$id", id.ToString());
        try
        {
            if (await command.ExecuteNonQueryAsync(cancellationToken) == 0) throw new KeyNotFoundException("SSH credential was not found.");
        }
        catch (SqliteException exception) when (exception.SqliteErrorCode == 19) { throw new InvalidOperationException($"An SSH credential named '{normalized}' already exists."); }
    }

    public async Task DeleteAsync(string ownerSubjectId, Guid id, CancellationToken cancellationToken = default)
    {
        ownerSubjectId = RequireOwner(ownerSubjectId);
        await using var connection = await OpenAsync(cancellationToken);
        var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM ssh_credentials WHERE owner_subject_id=$owner AND id=$id";
        command.Parameters.AddWithValue("$owner", ownerSubjectId);
        command.Parameters.AddWithValue("$id", id.ToString());
        if (await command.ExecuteNonQueryAsync(cancellationToken) == 0) throw new KeyNotFoundException("SSH credential was not found.");
    }

    private async Task<SqliteConnection> OpenAsync(CancellationToken cancellationToken)
    {
        var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    private static void AddParameters(SqliteCommand command, SshCredential credential)
    {
        command.Parameters.AddWithValue("$id", credential.Id.ToString());
        command.Parameters.AddWithValue("$owner", credential.OwnerSubjectId);
        command.Parameters.AddWithValue("$name", credential.Name);
        command.Parameters.AddWithValue("$algorithm", credential.Algorithm);
        command.Parameters.AddWithValue("$fingerprint", credential.Fingerprint);
        command.Parameters.AddWithValue("$passphrase", credential.RequiresPassphrase ? 1 : 0);
        command.Parameters.AddWithValue("$created", credential.CreatedAt.ToString("O"));
    }

    private static SshCredential Read(SqliteDataReader reader) => new(
        Guid.Parse(reader.GetString(0)), reader.GetString(1), reader.GetString(2), reader.GetString(3), reader.GetString(4), reader.GetBoolean(5),
        DateTimeOffset.Parse(reader.GetString(6)), reader.IsDBNull(7) ? null : DateTimeOffset.Parse(reader.GetString(7)));

    private static string RequireOwner(string ownerSubjectId) => string.IsNullOrWhiteSpace(ownerSubjectId) ? throw new ArgumentException("Owner subject ID is required.", nameof(ownerSubjectId)) : ownerSubjectId.Trim();
}
