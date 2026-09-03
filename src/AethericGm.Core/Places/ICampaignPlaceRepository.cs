namespace AethericGm.Core.Places;

public interface ICampaignPlaceRepository
{
    Task<IReadOnlyList<Place>> ListAsync(Guid campaignId, CancellationToken cancellationToken = default);
    Task<Place?> GetAsync(Guid campaignId, Guid placeId, CancellationToken cancellationToken = default);
    Task SaveAsync(Place place, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid campaignId, Guid placeId, CancellationToken cancellationToken = default);
}
