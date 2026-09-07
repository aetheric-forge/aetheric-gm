using System.Text.Json;
using AethericGm.Core.Entities;
using AethericGm.Core.People;
using Microsoft.Data.Sqlite;

namespace AethericGm.Infrastructure.People;

public sealed class SqliteCampaignEntityRepository(string connectionString) : ICampaignEntityRepository
{
    private const string Columns = "id,campaign_id,kind,name,notes,notes_secret,tags_json,role,status,place_id,created_at,updated_at";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken);
        var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS campaign_entities (
              id TEXT PRIMARY KEY,
              campaign_id TEXT NOT NULL,
              kind TEXT NOT NULL,
              name TEXT NOT NULL,
              notes TEXT,
              notes_secret INTEGER NOT NULL,
              tags_json TEXT NOT NULL,
              role TEXT,
              status TEXT,
              created_at TEXT NOT NULL,
              updated_at TEXT NOT NULL,
              FOREIGN KEY(campaign_id) REFERENCES campaigns(id) ON DELETE CASCADE
            );
            CREATE INDEX IF NOT EXISTS ix_campaign_entities_campaign ON campaign_entities(campaign_id, updated_at DESC);
            """;
        await command.ExecuteNonQueryAsync(cancellationToken);
        await AddColumnIfMissingAsync(connection, "place_id", "TEXT", cancellationToken);
    }

    public async Task<IReadOnlyList<CampaignEntity>> ListAsync(Guid campaignId, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken);
        var command = connection.CreateCommand();
        command.CommandText = $"SELECT {Columns} FROM campaign_entities WHERE campaign_id=$campaign ORDER BY updated_at DESC";
        command.Parameters.AddWithValue("$campaign", campaignId.ToString());
        var entities = new List<CampaignEntity>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken)) entities.Add(Read(reader));
        return entities;
    }

    public async Task<CampaignEntity?> GetAsync(Guid campaignId, Guid entityId, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken);
        var command = connection.CreateCommand();
        command.CommandText = $"SELECT {Columns} FROM campaign_entities WHERE campaign_id=$campaign AND id=$id";
        command.Parameters.AddWithValue("$campaign", campaignId.ToString());
        command.Parameters.AddWithValue("$id", entityId.ToString());
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? Read(reader) : null;
    }

    public async Task SaveAsync(CampaignEntity entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        await using var connection = await OpenAsync(cancellationToken);
        var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO campaign_entities(id,campaign_id,kind,name,notes,notes_secret,tags_json,role,status,place_id,created_at,updated_at)
            VALUES($id,$campaign,$kind,$name,$notes,$notesSecret,$tags,$role,$status,$placeId,$created,$updated)
            ON CONFLICT(id) DO UPDATE SET name=$name,notes=$notes,notes_secret=$notesSecret,tags_json=$tags,role=$role,status=$status,place_id=$placeId,updated_at=$updated;
            """;
        command.Parameters.AddWithValue("$id", entity.Id.ToString());
        command.Parameters.AddWithValue("$campaign", entity.CampaignId.ToString());
        command.Parameters.AddWithValue("$kind", entity.Kind.ToString());
        command.Parameters.AddWithValue("$name", entity.Name);
        command.Parameters.AddWithValue("$notes", (object?)entity.Notes ?? DBNull.Value);
        command.Parameters.AddWithValue("$notesSecret", entity.NotesAreSecret);
        command.Parameters.AddWithValue("$tags", JsonSerializer.Serialize(entity.Tags, JsonOptions));
        command.Parameters.AddWithValue("$role", (object?)entity.Role ?? DBNull.Value);
        command.Parameters.AddWithValue("$status", (object?)entity.Status ?? DBNull.Value);
        command.Parameters.AddWithValue("$placeId", (object?)entity.PlaceId?.ToString() ?? DBNull.Value);
        command.Parameters.AddWithValue("$created", entity.CreatedAt.ToString("O"));
        command.Parameters.AddWithValue("$updated", entity.UpdatedAt.ToString("O"));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid campaignId, Guid entityId, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken);
        var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM campaign_entities WHERE campaign_id=$campaign AND id=$id";
        command.Parameters.AddWithValue("$campaign", campaignId.ToString());
        command.Parameters.AddWithValue("$id", entityId.ToString());
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task<SqliteConnection> OpenAsync(CancellationToken cancellationToken)
    {
        var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        var foreignKeys = connection.CreateCommand();
        foreignKeys.CommandText = "PRAGMA foreign_keys=ON";
        await foreignKeys.ExecuteNonQueryAsync(cancellationToken);
        return connection;
    }

    private static async Task AddColumnIfMissingAsync(SqliteConnection connection, string column, string definition, CancellationToken ct)
    {
        var inspect = connection.CreateCommand(); inspect.CommandText = "PRAGMA table_info(campaign_entities)";
        await using var reader = await inspect.ExecuteReaderAsync(ct); var exists = false;
        while (await reader.ReadAsync(ct)) if (string.Equals(reader.GetString(1), column, StringComparison.Ordinal)) { exists = true; break; }
        await reader.DisposeAsync();
        if (!exists) { var alter = connection.CreateCommand(); alter.CommandText = $"ALTER TABLE campaign_entities ADD COLUMN {column} {definition}"; await alter.ExecuteNonQueryAsync(ct); }
    }

    private static CampaignEntity Read(SqliteDataReader reader)
    {
        var tags = JsonSerializer.Deserialize<List<string>>(reader.GetString(6), JsonOptions) ?? [];
        return CampaignEntity.Rehydrate(
            Guid.Parse(reader.GetString(0)), Guid.Parse(reader.GetString(1)), Enum.Parse<EntityKind>(reader.GetString(2)),
            reader.GetString(3), reader.IsDBNull(4) ? null : reader.GetString(4), reader.GetBoolean(5), tags,
            reader.IsDBNull(7) ? null : reader.GetString(7), reader.IsDBNull(8) ? null : reader.GetString(8),
            reader.IsDBNull(9) ? null : Guid.Parse(reader.GetString(9)),
            DateTimeOffset.Parse(reader.GetString(10)), DateTimeOffset.Parse(reader.GetString(11)));
    }
}
