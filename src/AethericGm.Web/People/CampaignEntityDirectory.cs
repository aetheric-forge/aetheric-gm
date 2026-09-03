using AethericGm.Core.Characters;
using AethericGm.Core.Entities;
using AethericGm.Core.Npcs;
using AethericGm.Core.People;
using AethericGm.Core.Places;

namespace AethericGm.Web.People;

public sealed record CampaignEntitySummary(EntityReference Reference, string Name, IReadOnlyList<string> Tags, string? Role, string? Status);

// Merges Npcs (Core.Npcs), People/Factions (Core.People), player Characters (Core.Characters), and Places
// (Core.Places) into one cross-kind view for search, filtering, and resolving a relationship's other
// endpoint. An Npc's Disposition fills the Role slot here for combined filtering only - CampaignNpc itself
// has no Role field and is not changed. Characters and Places have no Tags/Role/Status concept at all, so
// they always report empty/null for those.
public sealed class CampaignEntityDirectory(INpcRepository npcs, ICampaignEntityRepository entities, ICharacterRepository characters, ICampaignPlaceRepository places)
{
    public async Task<IReadOnlyList<CampaignEntitySummary>> ListAsync(Guid campaignId, CancellationToken cancellationToken = default)
    {
        var npcSummaries = (await npcs.ListAsync(campaignId, cancellationToken))
            .Select(npc => new CampaignEntitySummary(new EntityReference(EntityKind.Npc, npc.Id), npc.Name, npc.Tags, npc.Disposition, npc.Status));
        var entitySummaries = (await entities.ListAsync(campaignId, cancellationToken))
            .Select(entity => new CampaignEntitySummary(new EntityReference(entity.Kind, entity.Id), entity.Name, entity.Tags, entity.Role, entity.Status));
        var characterSummaries = (await characters.ListAsync(campaignId, cancellationToken))
            .Select(character => new CampaignEntitySummary(new EntityReference(EntityKind.Character, character.Id), character.Name, [], null, null));
        var placeSummaries = (await places.ListAsync(campaignId, cancellationToken))
            .Select(place => new CampaignEntitySummary(new EntityReference(EntityKind.Place, place.Id), place.Name, [], null, null));
        return npcSummaries.Concat(entitySummaries).Concat(characterSummaries).Concat(placeSummaries)
            .OrderBy(summary => summary.Name, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    public async Task<CampaignEntitySummary?> FindAsync(Guid campaignId, EntityReference reference, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(reference);
        switch (reference.Kind)
        {
            case EntityKind.Npc:
                var npc = await npcs.GetAsync(campaignId, reference.Id, cancellationToken);
                return npc is null ? null : new CampaignEntitySummary(reference, npc.Name, npc.Tags, npc.Disposition, npc.Status);
            case EntityKind.Character:
                var character = await characters.GetAsync(campaignId, reference.Id, cancellationToken);
                return character is null ? null : new CampaignEntitySummary(reference, character.Name, [], null, null);
            case EntityKind.Place:
                var place = await places.GetAsync(campaignId, reference.Id, cancellationToken);
                return place is null ? null : new CampaignEntitySummary(reference, place.Name, [], null, null);
            default:
                var entity = await entities.GetAsync(campaignId, reference.Id, cancellationToken);
                return entity is null || entity.Kind != reference.Kind ? null : new CampaignEntitySummary(reference, entity.Name, entity.Tags, entity.Role, entity.Status);
        }
    }
}
