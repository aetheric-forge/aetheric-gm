using AethericGm.Core.Campaigns;
using AethericGm.Core.Places;
using AethericGm.Infrastructure.Campaigns;
using AethericGm.Infrastructure.Places;

namespace AethericGm.Tests;

public sealed class SqliteCampaignPlaceRepositoryTests : IAsyncLifetime
{
    private readonly string path = Path.Combine(Path.GetTempPath(), $"aetheric-place-{Guid.NewGuid():N}.db");
    private SqliteCampaignPlaceRepository repository = null!;
    private Campaign campaign = null!;

    public async Task InitializeAsync()
    {
        var campaigns = new SqliteCampaignRepository($"Data Source={path}");
        await campaigns.InitializeAsync();
        campaign = Campaign.Create("Ashes", DateTimeOffset.UtcNow);
        await campaigns.SaveAsync(campaign);
        repository = new SqliteCampaignPlaceRepository($"Data Source={path}");
        await repository.InitializeAsync();
    }

    public Task DisposeAsync() { File.Delete(path); return Task.CompletedTask; }

    [Fact]
    public async Task Persists_a_nested_place()
    {
        var region = Place.Create(campaign.Id, "Coldharbor", null, DateTimeOffset.UtcNow);
        await repository.SaveAsync(region);
        var city = Place.Create(campaign.Id, "Ashport", region.Id, DateTimeOffset.UtcNow);
        await repository.SaveAsync(city);

        var reopened = await repository.GetAsync(campaign.Id, city.Id);
        Assert.NotNull(reopened);
        Assert.Equal(region.Id, reopened.ParentId);
        Assert.Equal(2, (await repository.ListAsync(campaign.Id)).Count);
    }

    [Fact]
    public async Task Delete_clears_the_parent_of_former_children_instead_of_deleting_them()
    {
        var region = Place.Create(campaign.Id, "Coldharbor", null, DateTimeOffset.UtcNow);
        await repository.SaveAsync(region);
        var city = Place.Create(campaign.Id, "Ashport", region.Id, DateTimeOffset.UtcNow);
        await repository.SaveAsync(city);

        await repository.DeleteAsync(campaign.Id, region.Id);

        Assert.Null(await repository.GetAsync(campaign.Id, region.Id));
        var reopenedCity = await repository.GetAsync(campaign.Id, city.Id);
        Assert.NotNull(reopenedCity);
        Assert.Null(reopenedCity.ParentId);
    }
}
