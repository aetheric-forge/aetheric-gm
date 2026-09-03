using AethericGm.Core.Entities;

namespace AethericGm.Core.Relationships;

public interface ICampaignRelationshipRepository
{
    Task<IReadOnlyList<Relationship>> ListForEntityAsync(Guid campaignId, EntityReference entity, CancellationToken cancellationToken = default);
    Task SaveAsync(Relationship relationship, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid campaignId, Guid relationshipId, CancellationToken cancellationToken = default);
}
