using AethericGm.Core.Campaigns;
using AethericGm.Core.Entities;
using AethericGm.Core.People;
using AethericGm.Infrastructure.Campaigns;
using AethericGm.Infrastructure.People;

namespace AethericGm.Tests;

public sealed class SqliteCampaignEntityRepositoryTests : IAsyncLifetime
{
    private readonly string path = Path.Combine(Path.GetTempPath(), $"aetheric-entity-{Guid.NewGuid():N}.db");
    private SqliteCampaignEntityRepository repository = null!;
    private Campaign campaign = null!;

    public async Task InitializeAsync()
    {
        var campaigns = new SqliteCampaignRepository($"Data Source={path}");
        await campaigns.InitializeAsync();
        campaign = Campaign.Create("Ashes", DateTimeOffset.UtcNow);
        await campaigns.SaveAsync(campaign);
        repository = new SqliteCampaignEntityRepository($"Data Source={path}");
        await repository.InitializeAsync();
    }

    public Task DisposeAsync() { File.Delete(path); return Task.CompletedTask; }

    [Fact]
    public async Task Persists_a_faction_with_secret_notes_and_tags()
    {
        var entity = CampaignEntity.Create(campaign.Id, EntityKind.Faction, "The Ashen Company", DateTimeOffset.UtcNow);
        entity.Update("The Ashen Company", "Secretly loyal to the crown.", true, ["mercenary", "guild"], "Guild", "Active", DateTimeOffset.UtcNow);
        await repository.SaveAsync(entity);

        var reopened = await repository.GetAsync(campaign.Id, entity.Id);
        Assert.NotNull(reopened);
        Assert.Equal(EntityKind.Faction, reopened.Kind);
        Assert.True(reopened.NotesAreSecret);
        Assert.Equal(["mercenary", "guild"], reopened.Tags);
        Assert.Equal(entity.Id, Assert.Single(await repository.ListAsync(campaign.Id)).Id);
    }

    [Fact]
    public async Task Delete_removes_the_entity()
    {
        var entity = CampaignEntity.Create(campaign.Id, EntityKind.Person, "A Contact", DateTimeOffset.UtcNow);
        await repository.SaveAsync(entity);
        await repository.DeleteAsync(campaign.Id, entity.Id);
        Assert.Null(await repository.GetAsync(campaign.Id, entity.Id));
    }
}
