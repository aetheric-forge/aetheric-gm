namespace AethericGm.Core.Campaigns;

public interface ICampaignRepository
{
    Task<IReadOnlyList<Campaign>> ListAsync(bool includeArchived = false, CancellationToken cancellationToken = default);
    Task<Campaign?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task SaveAsync(Campaign campaign, CancellationToken cancellationToken = default);
    Task<Guid?> GetSelectedIdAsync(CancellationToken cancellationToken = default);
    Task SetSelectedIdAsync(Guid? id, CancellationToken cancellationToken = default);
}
