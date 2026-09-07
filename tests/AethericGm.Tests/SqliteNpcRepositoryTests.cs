using AethericGm.Core.Campaigns;
using AethericGm.Core.Npcs;
using AethericGm.Core.Rules;
using AethericGm.Core.Rules.Records;
using AethericGm.Infrastructure.Campaigns;
using AethericGm.Infrastructure.Npcs;

namespace AethericGm.Tests;

public sealed class SqliteNpcRepositoryTests : IAsyncLifetime
{
    private readonly string path = Path.Combine(Path.GetTempPath(), $"aetheric-npc-{Guid.NewGuid():N}.db");
    private SqliteNpcRepository repository = null!;
    private Campaign campaign = null!;

    public async Task InitializeAsync()
    {
        var campaigns = new SqliteCampaignRepository($"Data Source={path}");
        await campaigns.InitializeAsync();
        campaign = Campaign.Create("Ashes", DateTimeOffset.UtcNow);
        await campaigns.SaveAsync(campaign);
        repository = new SqliteNpcRepository($"Data Source={path}");
        await repository.InitializeAsync();
    }

    public Task DisposeAsync() { File.Delete(path); return Task.CompletedTask; }

    [Fact]
    public async Task Persists_a_campaign_original_npc()
    {
        var placeId = Guid.NewGuid();
        var npc = CampaignNpc.Create(campaign.Id, "Grask", null, DateTimeOffset.UtcNow);
        npc.Update("Grask the Wary", "Keeps to the shadows.", ["goblin", "sentinel"], "Wary", "Old Watchtower", placeId, "Alive", DateTimeOffset.UtcNow);
        npc.UpdateResources([new NpcResource("HP", 9, 12)], DateTimeOffset.UtcNow);
        await repository.SaveAsync(npc);

        var reopened = await repository.GetAsync(campaign.Id, npc.Id);
        Assert.NotNull(reopened);
        Assert.Equal("Grask the Wary", reopened.Name);
        Assert.Null(reopened.Source);
        Assert.Equal(["goblin", "sentinel"], reopened.Tags);
        Assert.Equal(placeId, reopened.PlaceId);
        Assert.Equal(9, Assert.Single(reopened.Resources).Current);
        Assert.Equal(npc.Id, Assert.Single(await repository.ListAsync(campaign.Id)).Id);
    }

    [Fact]
    public async Task Persists_the_source_reference_for_an_npc_copied_from_a_package()
    {
        var source = new NpcSource(new RulesetReference("test", "1.0.0"), new RulesRecordReference("npc", "goblin"));
        var npc = CampaignNpc.Create(campaign.Id, "Goblin", source, DateTimeOffset.UtcNow);
        await repository.SaveAsync(npc);

        var reopened = await repository.GetAsync(campaign.Id, npc.Id);
        Assert.Equal(source, reopened!.Source);
    }

    [Fact]
    public async Task Delete_removes_the_npc()
    {
        var npc = CampaignNpc.Create(campaign.Id, "Grask", null, DateTimeOffset.UtcNow);
        await repository.SaveAsync(npc);
        await repository.DeleteAsync(campaign.Id, npc.Id);
        Assert.Null(await repository.GetAsync(campaign.Id, npc.Id));
    }
}
