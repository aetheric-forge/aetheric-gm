using AethericGm.Core.Campaigns;
using AethericGm.Infrastructure.Campaigns;
namespace AethericGm.Tests;
public class SqliteCampaignRepositoryTests : IAsyncLifetime
{
    private readonly string path = Path.Combine(Path.GetTempPath(), $"aetheric-gm-{Guid.NewGuid():N}.db"); private SqliteCampaignRepository repository = null!;
    public async Task InitializeAsync() { repository = new($"Data Source={path}"); await repository.InitializeAsync(); }
    public Task DisposeAsync() { File.Delete(path); return Task.CompletedTask; }
    [Fact] public async Task Persists_selection_and_hides_archived_by_default()
    {
        var campaign = Campaign.Create("Ashes", DateTimeOffset.UtcNow); await repository.SaveAsync(campaign); await repository.SetSelectedIdAsync(campaign.Id);
        var reopened = new SqliteCampaignRepository($"Data Source={path}"); await reopened.InitializeAsync(); Assert.Equal(campaign.Id, await reopened.GetSelectedIdAsync()); Assert.Single(await reopened.ListAsync());
        campaign.Archive(DateTimeOffset.UtcNow); await reopened.SaveAsync(campaign); Assert.Empty(await reopened.ListAsync()); Assert.Single(await reopened.ListAsync(true));
    }
}
