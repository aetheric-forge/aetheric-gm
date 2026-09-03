namespace AethericGm.Core.People;

public interface ICampaignEntityRepository
{
    Task<IReadOnlyList<CampaignEntity>> ListAsync(Guid campaignId, CancellationToken cancellationToken = default);
    Task<CampaignEntity?> GetAsync(Guid campaignId, Guid entityId, CancellationToken cancellationToken = default);
    Task SaveAsync(CampaignEntity entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid campaignId, Guid entityId, CancellationToken cancellationToken = default);
}
