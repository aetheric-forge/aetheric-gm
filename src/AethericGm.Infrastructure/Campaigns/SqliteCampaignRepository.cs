using AethericGm.Core.Campaigns;
using Microsoft.Data.Sqlite;
using AethericGm.Core.Rules;

namespace AethericGm.Infrastructure.Campaigns;

public sealed class SqliteCampaignRepository(string connectionString) : ICampaignRepository
{
    public async Task InitializeAsync(CancellationToken ct = default)
    {
        await using var connection = await OpenAsync(ct);
        var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS campaigns (
              id TEXT PRIMARY KEY, name TEXT NOT NULL, system TEXT NULL, setting TEXT NULL,
              summary TEXT NULL, created_at TEXT NOT NULL, updated_at TEXT NOT NULL, archived_at TEXT NULL,
              ruleset_id TEXT NULL, ruleset_version TEXT NULL
            );
            CREATE TABLE IF NOT EXISTS app_state (key TEXT PRIMARY KEY, value TEXT NULL);
            """;
        await command.ExecuteNonQueryAsync(ct);
        await AddColumnIfMissingAsync(connection, "ruleset_id", "TEXT NULL", ct);
        await AddColumnIfMissingAsync(connection, "ruleset_version", "TEXT NULL", ct);
    }

    public async Task<IReadOnlyList<Campaign>> ListAsync(bool includeArchived = false, CancellationToken ct = default)
    {
        await using var connection = await OpenAsync(ct);
        var command = connection.CreateCommand();
        command.CommandText = $"SELECT id,name,system,setting,summary,created_at,updated_at,archived_at,ruleset_id,ruleset_version FROM campaigns {(includeArchived ? "" : "WHERE archived_at IS NULL")} ORDER BY updated_at DESC";
        var result = new List<Campaign>();
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct)) result.Add(Read(reader));
        return result;
    }

    public async Task<Campaign?> GetAsync(Guid id, CancellationToken ct = default)
    {
        await using var connection = await OpenAsync(ct);
        var command = connection.CreateCommand();
        command.CommandText = "SELECT id,name,system,setting,summary,created_at,updated_at,archived_at,ruleset_id,ruleset_version FROM campaigns WHERE id=$id";
        command.Parameters.AddWithValue("$id", id.ToString());
        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? Read(reader) : null;
    }

    public async Task SaveAsync(Campaign campaign, CancellationToken ct = default)
    {
        await using var connection = await OpenAsync(ct);
        var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO campaigns(id,name,system,setting,summary,created_at,updated_at,archived_at,ruleset_id,ruleset_version)
            VALUES($id,$name,$system,$setting,$summary,$created,$updated,$archived,$rulesetId,$rulesetVersion)
            ON CONFLICT(id) DO UPDATE SET name=$name,system=$system,setting=$setting,summary=$summary,updated_at=$updated,archived_at=$archived,ruleset_id=$rulesetId,ruleset_version=$rulesetVersion;
            """;
        command.Parameters.AddWithValue("$id", campaign.Id.ToString());
        command.Parameters.AddWithValue("$name", campaign.Name);
        command.Parameters.AddWithValue("$system", (object?)campaign.System ?? DBNull.Value);
        command.Parameters.AddWithValue("$setting", (object?)campaign.Setting ?? DBNull.Value);
        command.Parameters.AddWithValue("$summary", (object?)campaign.Summary ?? DBNull.Value);
        command.Parameters.AddWithValue("$created", campaign.CreatedAt.ToString("O"));
        command.Parameters.AddWithValue("$updated", campaign.UpdatedAt.ToString("O"));
        command.Parameters.AddWithValue("$archived", campaign.ArchivedAt is { } archived ? archived.ToString("O") : DBNull.Value);
        command.Parameters.AddWithValue("$rulesetId", (object?)campaign.Ruleset?.Id ?? DBNull.Value);
        command.Parameters.AddWithValue("$rulesetVersion", (object?)campaign.Ruleset?.Version ?? DBNull.Value);
        await command.ExecuteNonQueryAsync(ct);
    }

    public async Task<Guid?> GetSelectedIdAsync(CancellationToken ct = default)
    {
        await using var connection = await OpenAsync(ct);
        var command = connection.CreateCommand();
        command.CommandText = "SELECT value FROM app_state WHERE key='selected_campaign'";
        var value = await command.ExecuteScalarAsync(ct) as string;
        return Guid.TryParse(value, out var id) ? id : null;
    }

    public async Task SetSelectedIdAsync(Guid? id, CancellationToken ct = default)
    {
        await using var connection = await OpenAsync(ct);
        var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO app_state(key,value) VALUES('selected_campaign',$value) ON CONFLICT(key) DO UPDATE SET value=$value";
        command.Parameters.AddWithValue("$value", id is null ? DBNull.Value : id.Value.ToString());
        await command.ExecuteNonQueryAsync(ct);
    }

    private async Task<SqliteConnection> OpenAsync(CancellationToken ct)
    {
        var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync(ct);
        return connection;
    }

    private static async Task AddColumnIfMissingAsync(SqliteConnection connection, string column, string definition, CancellationToken ct)
    {
        var inspect = connection.CreateCommand(); inspect.CommandText = "PRAGMA table_info(campaigns)";
        await using var reader = await inspect.ExecuteReaderAsync(ct); var exists = false;
        while (await reader.ReadAsync(ct)) if (string.Equals(reader.GetString(1), column, StringComparison.Ordinal)) { exists = true; break; }
        await reader.DisposeAsync();
        if (!exists) { var alter = connection.CreateCommand(); alter.CommandText = $"ALTER TABLE campaigns ADD COLUMN {column} {definition}"; await alter.ExecuteNonQueryAsync(ct); }
    }

    private static Campaign Read(SqliteDataReader r) => Campaign.Rehydrate(
        Guid.Parse(r.GetString(0)), r.GetString(1), r.IsDBNull(2) ? null : r.GetString(2),
        r.IsDBNull(3) ? null : r.GetString(3), r.IsDBNull(4) ? null : r.GetString(4),
        DateTimeOffset.Parse(r.GetString(5)), DateTimeOffset.Parse(r.GetString(6)),
        r.IsDBNull(7) ? null : DateTimeOffset.Parse(r.GetString(7)),
        r.IsDBNull(8) || r.IsDBNull(9) ? null : new RulesetReference(r.GetString(8), r.GetString(9)));
}
