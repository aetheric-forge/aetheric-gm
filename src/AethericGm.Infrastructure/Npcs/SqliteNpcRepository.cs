using System.Text.Json;
using AethericGm.Core.Npcs;
using AethericGm.Core.Rules;
using AethericGm.Core.Rules.Records;
using Microsoft.Data.Sqlite;

namespace AethericGm.Infrastructure.Npcs;

public sealed class SqliteNpcRepository(string connectionString) : INpcRepository
{
    private const string Columns =
        "id,campaign_id,source_ruleset_id,source_ruleset_version,source_record_type,source_record_key,name,notes,tags_json,disposition,location,status,resources_json,created_at,updated_at";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken);
        var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS npcs (
              id TEXT PRIMARY KEY,
              campaign_id TEXT NOT NULL,
              source_ruleset_id TEXT,
              source_ruleset_version TEXT,
              source_record_type TEXT,
              source_record_key TEXT,
              name TEXT NOT NULL,
              notes TEXT,
              tags_json TEXT NOT NULL,
              disposition TEXT,
              location TEXT,
              status TEXT,
              resources_json TEXT NOT NULL,
              created_at TEXT NOT NULL,
              updated_at TEXT NOT NULL,
              FOREIGN KEY(campaign_id) REFERENCES campaigns(id) ON DELETE CASCADE
            );
            CREATE INDEX IF NOT EXISTS ix_npcs_campaign ON npcs(campaign_id, updated_at DESC);
            """;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CampaignNpc>> ListAsync(Guid campaignId, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken);
        var command = connection.CreateCommand();
        command.CommandText = $"SELECT {Columns} FROM npcs WHERE campaign_id=$campaign ORDER BY updated_at DESC";
        command.Parameters.AddWithValue("$campaign", campaignId.ToString());
        var npcs = new List<CampaignNpc>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken)) npcs.Add(Read(reader));
        return npcs;
    }

    public async Task<CampaignNpc?> GetAsync(Guid campaignId, Guid npcId, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken);
        var command = connection.CreateCommand();
        command.CommandText = $"SELECT {Columns} FROM npcs WHERE campaign_id=$campaign AND id=$id";
        command.Parameters.AddWithValue("$campaign", campaignId.ToString());
        command.Parameters.AddWithValue("$id", npcId.ToString());
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? Read(reader) : null;
    }

    public async Task SaveAsync(CampaignNpc npc, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(npc);
        await using var connection = await OpenAsync(cancellationToken);
        var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO npcs(id,campaign_id,source_ruleset_id,source_ruleset_version,source_record_type,source_record_key,
              name,notes,tags_json,disposition,location,status,resources_json,created_at,updated_at)
            VALUES($id,$campaign,$rulesetId,$rulesetVersion,$recordType,$recordKey,
              $name,$notes,$tags,$disposition,$location,$status,$resources,$created,$updated)
            ON CONFLICT(id) DO UPDATE SET name=$name,notes=$notes,tags_json=$tags,disposition=$disposition,
              location=$location,status=$status,resources_json=$resources,updated_at=$updated;
            """;
        command.Parameters.AddWithValue("$id", npc.Id.ToString());
        command.Parameters.AddWithValue("$campaign", npc.CampaignId.ToString());
        command.Parameters.AddWithValue("$rulesetId", (object?)npc.Source?.Ruleset.Id ?? DBNull.Value);
        command.Parameters.AddWithValue("$rulesetVersion", (object?)npc.Source?.Ruleset.Version ?? DBNull.Value);
        command.Parameters.AddWithValue("$recordType", (object?)npc.Source?.Record.RecordType ?? DBNull.Value);
        command.Parameters.AddWithValue("$recordKey", (object?)npc.Source?.Record.Key ?? DBNull.Value);
        command.Parameters.AddWithValue("$name", npc.Name);
        command.Parameters.AddWithValue("$notes", (object?)npc.Notes ?? DBNull.Value);
        command.Parameters.AddWithValue("$tags", JsonSerializer.Serialize(npc.Tags, JsonOptions));
        command.Parameters.AddWithValue("$disposition", (object?)npc.Disposition ?? DBNull.Value);
        command.Parameters.AddWithValue("$location", (object?)npc.Location ?? DBNull.Value);
        command.Parameters.AddWithValue("$status", (object?)npc.Status ?? DBNull.Value);
        command.Parameters.AddWithValue("$resources", JsonSerializer.Serialize(npc.Resources, JsonOptions));
        command.Parameters.AddWithValue("$created", npc.CreatedAt.ToString("O"));
        command.Parameters.AddWithValue("$updated", npc.UpdatedAt.ToString("O"));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid campaignId, Guid npcId, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken);
        var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM npcs WHERE campaign_id=$campaign AND id=$id";
        command.Parameters.AddWithValue("$campaign", campaignId.ToString());
        command.Parameters.AddWithValue("$id", npcId.ToString());
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

    private static CampaignNpc Read(SqliteDataReader reader)
    {
        var source = reader.IsDBNull(2) ? null : new NpcSource(
            new RulesetReference(reader.GetString(2), reader.GetString(3)),
            new RulesRecordReference(reader.GetString(4), reader.GetString(5)));
        var tags = JsonSerializer.Deserialize<List<string>>(reader.GetString(8), JsonOptions) ?? [];
        var resources = JsonSerializer.Deserialize<List<NpcResource>>(reader.GetString(12), JsonOptions) ?? [];
        return CampaignNpc.Rehydrate(
            Guid.Parse(reader.GetString(0)), Guid.Parse(reader.GetString(1)), source,
            reader.GetString(6), reader.IsDBNull(7) ? null : reader.GetString(7), tags,
            reader.IsDBNull(9) ? null : reader.GetString(9), reader.IsDBNull(10) ? null : reader.GetString(10),
            reader.IsDBNull(11) ? null : reader.GetString(11), resources,
            DateTimeOffset.Parse(reader.GetString(13)), DateTimeOffset.Parse(reader.GetString(14)));
    }
}
