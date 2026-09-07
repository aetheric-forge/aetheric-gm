using System.Text.Json;
using AethericGm.Core.Campaigns;
using AethericGm.Core.Characters;
using AethericGm.Core.Rules;
using AethericGm.Core.Rules.Records;
using AethericGm.Infrastructure.Campaigns;
using AethericGm.Infrastructure.Characters;

namespace AethericGm.Tests;

public sealed class SqliteCharacterRepositoryTests : IAsyncLifetime
{
    private readonly string path = Path.Combine(Path.GetTempPath(), $"aetheric-character-{Guid.NewGuid():N}.db");
    private SqliteCharacterRepository repository = null!;
    private Campaign campaign = null!;

    public async Task InitializeAsync()
    {
        var campaigns = new SqliteCampaignRepository($"Data Source={path}");
        await campaigns.InitializeAsync();
        campaign = Campaign.Create("Ashes", DateTimeOffset.UtcNow);
        await campaigns.SaveAsync(campaign);
        repository = new SqliteCharacterRepository($"Data Source={path}");
        await repository.InitializeAsync();
    }

    public Task DisposeAsync() { File.Delete(path); return Task.CompletedTask; }

    [Fact]
    public async Task Persists_campaign_owned_character_values_and_pinned_ruleset()
    {
        var values = new Dictionary<string, JsonElement>
        {
            ["name"] = JsonSerializer.SerializeToElement("Ember"),
            ["ancestry"] = JsonSerializer.SerializeToElement(new RulesRecordReference("ancestry", "elf"))
        };
        var character = Character.Create(campaign.Id, new RulesetReference("test", "1.0.0"), values, DateTimeOffset.UtcNow);
        await repository.SaveAsync(character);
        var reopened = await repository.GetAsync(campaign.Id, character.Id);
        Assert.NotNull(reopened);
        Assert.Equal("Ember", reopened.Name);
        Assert.Equal(character.Ruleset, reopened.Ruleset);
        Assert.Equal("elf", reopened.Values["ancestry"].GetProperty("key").GetString());
        Assert.Equal(character.Id, Assert.Single(await repository.ListAsync(campaign.Id)).Id);
        Assert.Null(await repository.GetAsync(Guid.NewGuid(), character.Id));
    }
}
