using AethericGm.Core.Sessions;
using Microsoft.Data.Sqlite;
using System.Globalization;

namespace AethericGm.Infrastructure.Sessions;

public sealed class SqliteSessionNotebookRepository(string connectionString, TimeProvider? timeProvider = null) : ISessionNotebookRepository
{
    private readonly TimeProvider clock = timeProvider ?? TimeProvider.System;
    private const string Columns = "id,campaign_id,title,markdown,planned_date,status,revision,updated_at,author";

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        await using var connection = await OpenAsync(ct);
        using var transaction = connection.BeginTransaction();
        using var command = connection.CreateCommand(); command.Transaction = transaction;
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS session_schema_versions(version INTEGER PRIMARY KEY);
            CREATE TABLE IF NOT EXISTS session_notebooks (
                id TEXT PRIMARY KEY, campaign_id TEXT NOT NULL REFERENCES campaigns(id),
                title TEXT NOT NULL, markdown TEXT NOT NULL, planned_date TEXT NULL,
                status INTEGER NOT NULL CHECK(status BETWEEN 0 AND 2), revision INTEGER NOT NULL CHECK(revision > 0),
                updated_at TEXT NOT NULL, author TEXT NOT NULL
            );
            CREATE INDEX IF NOT EXISTS ix_session_campaign ON session_notebooks(campaign_id,updated_at);
            CREATE TABLE IF NOT EXISTS session_notebook_revisions (
                id TEXT NOT NULL REFERENCES session_notebooks(id), campaign_id TEXT NOT NULL,
                title TEXT NOT NULL, markdown TEXT NOT NULL, planned_date TEXT NULL,
                status INTEGER NOT NULL, revision INTEGER NOT NULL, updated_at TEXT NOT NULL, author TEXT NOT NULL,
                PRIMARY KEY(id,revision)
            );
            INSERT OR IGNORE INTO session_schema_versions(version) VALUES(1);
            """;
        await command.ExecuteNonQueryAsync(ct);
        transaction.Commit();
    }

    public async Task<IReadOnlyList<SessionNotebook>> ListAsync(Guid campaignId, CancellationToken ct = default)
    {
        await using var connection = await OpenAsync(ct);
        using var command = connection.CreateCommand();
        command.CommandText = $"SELECT {Columns} FROM session_notebooks WHERE campaign_id=$campaign ORDER BY updated_at DESC,id";
        command.Parameters.AddWithValue("$campaign", campaignId.ToString());
        var result = new List<SessionNotebook>();
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct)) result.Add(Read(reader));
        return result;
    }

    public Task<SessionNotebook?> GetAsync(Guid campaignId, Guid sessionId, CancellationToken ct = default) => ReadOneAsync(campaignId, sessionId, null, ct);
    public Task<SessionNotebook?> GetRevisionAsync(Guid campaignId, Guid sessionId, int revision, CancellationToken ct = default) => ReadOneAsync(campaignId, sessionId, revision, ct);

    private async Task<SessionNotebook?> ReadOneAsync(Guid campaignId, Guid sessionId, int? revision, CancellationToken ct)
    {
        await using var connection = await OpenAsync(ct);
        using var command = connection.CreateCommand();
        command.CommandText = $"SELECT {Columns} FROM {(revision is null ? "session_notebooks" : "session_notebook_revisions")} WHERE campaign_id=$campaign AND id=$id" + (revision is null ? "" : " AND revision=$revision");
        command.Parameters.AddWithValue("$campaign", campaignId.ToString()); command.Parameters.AddWithValue("$id", sessionId.ToString());
        if (revision is not null) command.Parameters.AddWithValue("$revision", revision.Value);
        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? Read(reader) : null;
    }

    public async Task<SessionNotebook> SaveAsync(SessionNotebook notebook, CancellationToken ct = default)
    {
        await using var connection = await OpenAsync(ct);
        using var transaction = connection.BeginTransaction();
        using var command = connection.CreateCommand(); command.Transaction = transaction;
        var saved = new SessionNotebook(notebook.Id, notebook.CampaignId, notebook.Title, notebook.Markdown,
            notebook.PlannedDate, notebook.Status, checked(notebook.Revision + 1), clock.GetUtcNow(), notebook.AuthorSubjectId);
        command.CommandText = notebook.Revision == 0
            ? $"INSERT INTO session_notebooks({Columns}) VALUES($id,$campaign,$title,$markdown,$date,$status,$revision,$updated,$author) ON CONFLICT(id) DO NOTHING"
            : "UPDATE session_notebooks SET title=$title,markdown=$markdown,planned_date=$date,status=$status,revision=$revision,updated_at=$updated,author=$author WHERE id=$id AND campaign_id=$campaign AND revision=$expected";
        Bind(command, saved); command.Parameters.AddWithValue("$expected", notebook.Revision);
        if (await command.ExecuteNonQueryAsync(ct) != 1) throw new SessionConflictException();
        command.CommandText = $"INSERT INTO session_notebook_revisions({Columns}) VALUES($id,$campaign,$title,$markdown,$date,$status,$revision,$updated,$author)";
        await command.ExecuteNonQueryAsync(ct);
        transaction.Commit();
        return saved;
    }

    public async Task<IReadOnlyList<SessionRevisionSummary>> ListRevisionsAsync(Guid campaignId, Guid sessionId, CancellationToken ct = default)
    {
        await using var connection = await OpenAsync(ct);
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT revision,updated_at,author,title,status FROM session_notebook_revisions WHERE campaign_id=$campaign AND id=$id ORDER BY revision DESC";
        command.Parameters.AddWithValue("$campaign", campaignId.ToString()); command.Parameters.AddWithValue("$id", sessionId.ToString());
        var result = new List<SessionRevisionSummary>();
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct)) result.Add(new(reader.GetInt32(0), DateTimeOffset.Parse(reader.GetString(1), CultureInfo.InvariantCulture), reader.GetString(2), reader.GetString(3), (SessionStatus)reader.GetInt32(4)));
        return result;
    }

    private static void Bind(SqliteCommand command, SessionNotebook notebook)
    {
        command.Parameters.AddWithValue("$id", notebook.Id.ToString()); command.Parameters.AddWithValue("$campaign", notebook.CampaignId.ToString());
        command.Parameters.AddWithValue("$title", notebook.Title); command.Parameters.AddWithValue("$markdown", notebook.Markdown);
        command.Parameters.AddWithValue("$date", (object?)notebook.PlannedDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? DBNull.Value);
        command.Parameters.AddWithValue("$status", (int)notebook.Status); command.Parameters.AddWithValue("$revision", notebook.Revision);
        command.Parameters.AddWithValue("$updated", notebook.UpdatedAt.ToString("O")); command.Parameters.AddWithValue("$author", notebook.AuthorSubjectId);
    }
    private static SessionNotebook Read(SqliteDataReader reader) => new(Guid.Parse(reader.GetString(0)), Guid.Parse(reader.GetString(1)),
        reader.GetString(2), reader.GetString(3), reader.IsDBNull(4) ? null : DateOnly.Parse(reader.GetString(4), CultureInfo.InvariantCulture),
        (SessionStatus)reader.GetInt32(5), reader.GetInt32(6), DateTimeOffset.Parse(reader.GetString(7), CultureInfo.InvariantCulture), reader.GetString(8));
    private async Task<SqliteConnection> OpenAsync(CancellationToken ct)
    {
        var options = new SqliteConnectionStringBuilder(connectionString) { ForeignKeys = true };
        var connection = new SqliteConnection(options.ToString());
        try { await connection.OpenAsync(ct); return connection; }
        catch { await connection.DisposeAsync(); throw; }
    }
}
