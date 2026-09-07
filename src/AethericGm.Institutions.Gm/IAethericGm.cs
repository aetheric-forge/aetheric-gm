using AethericGm.Core.Npcs;
using AethericGm.Core.People;
using AethericGm.Core.Places;
using AethericGm.Core.Relationships;
using AethericGm.Core.Campaigns;
using AethericGm.Core.Characters;
using AethericGm.Core.Dice;
using AethericGm.Core.Profiles;
using AethericGm.Core.Rules;
using AethericGm.Core.Rules.CharacterSheets;
using AethericGm.Core.Rules.Packages;
using AethericForge.Runtime.Abstractions.Interfaces.Institutions;

namespace AethericGm.Institutions.Gm;

/// <summary>Game capabilities contributed to a host campus.</summary>
public interface IAethericGm : IInstitution
{
    ICampaignRepository Campaigns { get; }
    ICharacterRepository Characters { get; }
    IRulesCatalog Rules { get; }
    ICharacterSheetDefinitionStore CharacterSheets { get; }
    IRulesPackageInstaller Packages { get; }
    ISshCredentialService Credentials { get; }
    INpcRepository Npcs { get; }
    ICampaignEntityRepository People { get; }
    ICampaignPlaceRepository Places { get; }
    ICampaignRelationshipRepository Relationships { get; }
    IDiceRoller Dice { get; }
}
