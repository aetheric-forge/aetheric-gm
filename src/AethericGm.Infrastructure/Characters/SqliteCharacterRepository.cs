using System.Text.Json;
using AethericGm.Core.Characters;
using AethericGm.Core.Rules;
using Microsoft.Data.Sqlite;

namespace AethericGm.Infrastructure.Characters;

public sealed class SqliteCharacterRepository(string connectionString) : ICharacterRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken);
        var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS characters (
              id TEXT PRIMARY KEY,
              campaign_id TEXT NOT NULL,
              ruleset_id TEXT NOT NULL,
              ruleset_version TEXT NOT NULL,
              values_json TEXT NOT NULL,
              created_at TEXT NOT NULL,
              updated_at TEXT NOT NULL,
              FOREIGN KEY(campaign_id) REFERENCES campaigns(id) ON DELETE CASCADE
            );
            CREATE INDEX IF NOT EXISTS ix_characters_campaign ON characters(campaign_id, updated_at DESC);
            """;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Character>> ListAsync(Guid campaignId, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken);
        var command = connection.CreateCommand();
        command.CommandText = "SELECT id,campaign_id,ruleset_id,ruleset_version,values_json,created_at,updated_at FROM characters WHERE campaign_id=$campaign ORDER BY updated_at DESC";
        command.Parameters.AddWithValue("$campaign", campaignId.ToString());
        var characters = new List<Character>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken)) characters.Add(Read(reader));
        return characters;
    }

    public async Task<Character?> GetAsync(Guid campaignId, Guid characterId, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken);
        var command = connection.CreateCommand();
        command.CommandText = "SELECT id,campaign_id,ruleset_id,ruleset_version,values_json,created_at,updated_at FROM characters WHERE campaign_id=$campaign AND id=$id";
        command.Parameters.AddWithValue("$campaign", campaignId.ToString());
        command.Parameters.AddWithValue("$id", characterId.ToString());
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? Read(reader) : null;
    }

    public async Task SaveAsync(Character character, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken);
        var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO characters(id,campaign_id,ruleset_id,ruleset_version,values_json,created_at,updated_at)
            VALUES($id,$campaign,$ruleset,$version,$values,$created,$updated)
            ON CONFLICT(id) DO UPDATE SET values_json=$values,updated_at=$updated;
            """;
        command.Parameters.AddWithValue("$id", character.Id.ToString());
        command.Parameters.AddWithValue("$campaign", character.CampaignId.ToString());
        command.Parameters.AddWithValue("$ruleset", character.Ruleset.Id);
        command.Parameters.AddWithValue("$version", character.Ruleset.Version);
        command.Parameters.AddWithValue("$values", JsonSerializer.Serialize(character.Values, JsonOptions));
        command.Parameters.AddWithValue("$created", character.CreatedAt.ToString("O"));
        command.Parameters.AddWithValue("$updated", character.UpdatedAt.ToString("O"));
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

    private static Character Read(SqliteDataReader reader)
    {
        var values = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(reader.GetString(4), JsonOptions)
            ?? throw new InvalidDataException("Stored character values are empty.");
        return Character.Rehydrate(Guid.Parse(reader.GetString(0)), Guid.Parse(reader.GetString(1)),
            new RulesetReference(reader.GetString(2), reader.GetString(3)), values,
            DateTimeOffset.Parse(reader.GetString(5)), DateTimeOffset.Parse(reader.GetString(6)));
    }
}
