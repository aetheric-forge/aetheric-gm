using AethericGm.Core.Campaigns;
using AethericGm.Core.Entities;
using AethericGm.Core.Npcs;
using AethericGm.Core.Relationships;
using AethericGm.Infrastructure.Campaigns;
using AethericGm.Infrastructure.Npcs;
using AethericGm.Infrastructure.Relationships;

namespace AethericGm.Tests;

public sealed class SqliteRelationshipRepositoryTests : IAsyncLifetime
{
    private readonly string path = Path.Combine(Path.GetTempPath(), $"aetheric-relationship-{Guid.NewGuid():N}.db");
    private SqliteRelationshipRepository repository = null!;
    private SqliteNpcRepository npcRepository = null!;
    private Campaign campaign = null!;

    public async Task InitializeAsync()
    {
        var campaigns = new SqliteCampaignRepository($"Data Source={path}");
        await campaigns.InitializeAsync();
        campaign = Campaign.Create("Ashes", DateTimeOffset.UtcNow);
        await campaigns.SaveAsync(campaign);
        repository = new SqliteRelationshipRepository($"Data Source={path}");
        await repository.InitializeAsync();
        npcRepository = new SqliteNpcRepository($"Data Source={path}");
        await npcRepository.InitializeAsync();
    }

    public Task DisposeAsync() { File.Delete(path); return Task.CompletedTask; }

    [Fact]
    public async Task ListForEntityAsync_finds_a_relationship_from_either_endpoint()
    {
        var from = new EntityReference(EntityKind.Person, Guid.NewGuid());
        var to = new EntityReference(EntityKind.Faction, Guid.NewGuid());
        var relationship = Relationship.Create(campaign.Id, from, to, "member of", false, false, DateTimeOffset.UtcNow);
        await repository.SaveAsync(relationship);

        Assert.Equal(relationship.Id, Assert.Single(await repository.ListForEntityAsync(campaign.Id, from)).Id);
        Assert.Equal(relationship.Id, Assert.Single(await repository.ListForEntityAsync(campaign.Id, to)).Id);
        Assert.Empty(await repository.ListForEntityAsync(campaign.Id, new EntityReference(EntityKind.Npc, Guid.NewGuid())));
    }

    [Fact]
    public async Task Relationship_survives_as_a_dangling_reference_after_its_target_npc_is_removed()
    {
        var npc = CampaignNpc.Create(campaign.Id, "Grask", null, DateTimeOffset.UtcNow);
        await npcRepository.SaveAsync(npc);
        var npcRef = new EntityReference(EntityKind.Npc, npc.Id);
        var person = new EntityReference(EntityKind.Person, Guid.NewGuid());
        var relationship = Relationship.Create(campaign.Id, person, npcRef, "rival of", true, false, DateTimeOffset.UtcNow);
        await repository.SaveAsync(relationship);

        await npcRepository.DeleteAsync(campaign.Id, npc.Id);

        var reopened = Assert.Single(await repository.ListForEntityAsync(campaign.Id, person));
        Assert.Equal(npcRef, reopened.To);
        Assert.Null(await npcRepository.GetAsync(campaign.Id, npc.Id));
    }

    [Fact]
    public async Task Delete_removes_the_relationship()
    {
        var relationship = Relationship.Create(campaign.Id, new EntityReference(EntityKind.Person, Guid.NewGuid()),
            new EntityReference(EntityKind.Faction, Guid.NewGuid()), "ally of", true, false, DateTimeOffset.UtcNow);
        await repository.SaveAsync(relationship);
        await repository.DeleteAsync(campaign.Id, relationship.Id);
        Assert.Empty(await repository.ListForEntityAsync(campaign.Id, relationship.From));
    }
}
