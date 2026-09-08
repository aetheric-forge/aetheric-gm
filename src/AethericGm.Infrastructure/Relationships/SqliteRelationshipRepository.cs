using AethericGm.Core.Entities;
using AethericGm.Core.Relationships;
using Microsoft.Data.Sqlite;

namespace AethericGm.Infrastructure.Relationships;

public sealed class SqliteRelationshipRepository(string connectionString) : ICampaignRelationshipRepository
{
    private const string Columns = "id,campaign_id,from_kind,from_id,to_kind,to_id,label,is_symmetric,is_secret,created_at,updated_at";

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken);
        var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS relationships (
              id TEXT PRIMARY KEY,
              campaign_id TEXT NOT NULL,
              from_kind TEXT NOT NULL,
              from_id TEXT NOT NULL,
              to_kind TEXT NOT NULL,
              to_id TEXT NOT NULL,
              label TEXT NOT NULL,
              is_symmetric INTEGER NOT NULL,
              is_secret INTEGER NOT NULL,
              created_at TEXT NOT NULL,
              updated_at TEXT NOT NULL,
              FOREIGN KEY(campaign_id) REFERENCES campaigns(id) ON DELETE CASCADE
            );
            CREATE INDEX IF NOT EXISTS ix_relationships_from ON relationships(campaign_id, from_kind, from_id);
            CREATE INDEX IF NOT EXISTS ix_relationships_to ON relationships(campaign_id, to_kind, to_id);
            """;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Relationship>> ListForEntityAsync(Guid campaignId, EntityReference entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        await using var connection = await OpenAsync(cancellationToken);
        var command = connection.CreateCommand();
        command.CommandText = $"""
            SELECT {Columns} FROM relationships
            WHERE campaign_id=$campaign AND ((from_kind=$kind AND from_id=$id) OR (to_kind=$kind AND to_id=$id))
            ORDER BY created_at
            """;
        command.Parameters.AddWithValue("$campaign", campaignId.ToString());
        command.Parameters.AddWithValue("$kind", entity.Kind.ToString());
        command.Parameters.AddWithValue("$id", entity.Id.ToString());
        var relationships = new List<Relationship>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken)) relationships.Add(Read(reader));
        return relationships;
    }

    public async Task SaveAsync(Relationship relationship, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(relationship);
        await using var connection = await OpenAsync(cancellationToken);
        var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO relationships(id,campaign_id,from_kind,from_id,to_kind,to_id,label,is_symmetric,is_secret,created_at,updated_at)
            VALUES($id,$campaign,$fromKind,$fromId,$toKind,$toId,$label,$symmetric,$secret,$created,$updated)
            ON CONFLICT(id) DO UPDATE SET label=$label,is_symmetric=$symmetric,is_secret=$secret,updated_at=$updated;
            """;
        command.Parameters.AddWithValue("$id", relationship.Id.ToString());
        command.Parameters.AddWithValue("$campaign", relationship.CampaignId.ToString());
        command.Parameters.AddWithValue("$fromKind", relationship.From.Kind.ToString());
        command.Parameters.AddWithValue("$fromId", relationship.From.Id.ToString());
        command.Parameters.AddWithValue("$toKind", relationship.To.Kind.ToString());
        command.Parameters.AddWithValue("$toId", relationship.To.Id.ToString());
        command.Parameters.AddWithValue("$label", relationship.Label);
        command.Parameters.AddWithValue("$symmetric", relationship.IsSymmetric);
        command.Parameters.AddWithValue("$secret", relationship.IsSecret);
        command.Parameters.AddWithValue("$created", relationship.CreatedAt.ToString("O"));
        command.Parameters.AddWithValue("$updated", relationship.UpdatedAt.ToString("O"));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid campaignId, Guid relationshipId, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken);
        var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM relationships WHERE campaign_id=$campaign AND id=$id";
        command.Parameters.AddWithValue("$campaign", campaignId.ToString());
        command.Parameters.AddWithValue("$id", relationshipId.ToString());
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

    private static Relationship Read(SqliteDataReader reader) => Relationship.Rehydrate(
        Guid.Parse(reader.GetString(0)), Guid.Parse(reader.GetString(1)),
        new EntityReference(Enum.Parse<EntityKind>(reader.GetString(2)), Guid.Parse(reader.GetString(3))),
        new EntityReference(Enum.Parse<EntityKind>(reader.GetString(4)), Guid.Parse(reader.GetString(5))),
        reader.GetString(6), reader.GetBoolean(7), reader.GetBoolean(8),
        DateTimeOffset.Parse(reader.GetString(9)), DateTimeOffset.Parse(reader.GetString(10)));
}
