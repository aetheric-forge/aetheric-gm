namespace AethericGm.Core.Npcs;

public interface INpcRepository
{
    Task<IReadOnlyList<CampaignNpc>> ListAsync(Guid campaignId, CancellationToken cancellationToken = default);
    Task<CampaignNpc?> GetAsync(Guid campaignId, Guid npcId, CancellationToken cancellationToken = default);
    Task SaveAsync(CampaignNpc npc, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid campaignId, Guid npcId, CancellationToken cancellationToken = default);
}
