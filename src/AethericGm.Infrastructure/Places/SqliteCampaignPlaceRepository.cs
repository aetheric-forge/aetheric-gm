using AethericGm.Core.Places;
using Microsoft.Data.Sqlite;

namespace AethericGm.Infrastructure.Places;

public sealed class SqliteCampaignPlaceRepository(string connectionString) : ICampaignPlaceRepository
{
    private const string Columns = "id,campaign_id,name,parent_id,notes,created_at,updated_at";

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken);
        var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS places (
              id TEXT PRIMARY KEY,
              campaign_id TEXT NOT NULL,
              name TEXT NOT NULL,
              parent_id TEXT,
              notes TEXT,
              created_at TEXT NOT NULL,
              updated_at TEXT NOT NULL,
              FOREIGN KEY(campaign_id) REFERENCES campaigns(id) ON DELETE CASCADE
            );
            CREATE INDEX IF NOT EXISTS ix_places_campaign ON places(campaign_id, updated_at DESC);
            CREATE INDEX IF NOT EXISTS ix_places_parent ON places(campaign_id, parent_id);
            """;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Place>> ListAsync(Guid campaignId, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken);
        var command = connection.CreateCommand();
        command.CommandText = $"SELECT {Columns} FROM places WHERE campaign_id=$campaign ORDER BY name COLLATE NOCASE";
        command.Parameters.AddWithValue("$campaign", campaignId.ToString());
        var places = new List<Place>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken)) places.Add(Read(reader));
        return places;
    }

    public async Task<Place?> GetAsync(Guid campaignId, Guid placeId, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken);
        var command = connection.CreateCommand();
        command.CommandText = $"SELECT {Columns} FROM places WHERE campaign_id=$campaign AND id=$id";
        command.Parameters.AddWithValue("$campaign", campaignId.ToString());
        command.Parameters.AddWithValue("$id", placeId.ToString());
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? Read(reader) : null;
    }

    public async Task SaveAsync(Place place, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(place);
        await using var connection = await OpenAsync(cancellationToken);
        var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO places(id,campaign_id,name,parent_id,notes,created_at,updated_at)
            VALUES($id,$campaign,$name,$parent,$notes,$created,$updated)
            ON CONFLICT(id) DO UPDATE SET name=$name,parent_id=$parent,notes=$notes,updated_at=$updated;
            """;
        command.Parameters.AddWithValue("$id", place.Id.ToString());
        command.Parameters.AddWithValue("$campaign", place.CampaignId.ToString());
        command.Parameters.AddWithValue("$name", place.Name);
        command.Parameters.AddWithValue("$parent", (object?)place.ParentId?.ToString() ?? DBNull.Value);
        command.Parameters.AddWithValue("$notes", (object?)place.Notes ?? DBNull.Value);
        command.Parameters.AddWithValue("$created", place.CreatedAt.ToString("O"));
        command.Parameters.AddWithValue("$updated", place.UpdatedAt.ToString("O"));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid campaignId, Guid placeId, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken);
        var clearChildren = connection.CreateCommand();
        clearChildren.CommandText = "UPDATE places SET parent_id=NULL WHERE campaign_id=$campaign AND parent_id=$id";
        clearChildren.Parameters.AddWithValue("$campaign", campaignId.ToString());
        clearChildren.Parameters.AddWithValue("$id", placeId.ToString());
        await clearChildren.ExecuteNonQueryAsync(cancellationToken);

        var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM places WHERE campaign_id=$campaign AND id=$id";
        command.Parameters.AddWithValue("$campaign", campaignId.ToString());
        command.Parameters.AddWithValue("$id", placeId.ToString());
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

    private static Place Read(SqliteDataReader reader) => Place.Rehydrate(
        Guid.Parse(reader.GetString(0)), Guid.Parse(reader.GetString(1)), reader.GetString(2),
        reader.IsDBNull(3) ? null : Guid.Parse(reader.GetString(3)), reader.IsDBNull(4) ? null : reader.GetString(4),
        DateTimeOffset.Parse(reader.GetString(5)), DateTimeOffset.Parse(reader.GetString(6)));
}
