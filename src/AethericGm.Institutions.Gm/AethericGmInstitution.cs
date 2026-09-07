using AethericGm.Core.Campaigns;
using AethericGm.Core.Characters;
using AethericGm.Core.Dice;
using AethericGm.Core.Profiles;
using AethericGm.Core.Rules;
using AethericGm.Core.Rules.CharacterSheets;
using AethericGm.Core.Rules.Packages;
using AethericForge.Runtime.Models.Institutions;

namespace AethericGm.Institutions.Gm;

public sealed class AethericGmInstitution(
    AethericGmContext context,
    ICampaignRepository campaigns,
    ICharacterRepository characters,
    IRulesCatalog rules,
    ICharacterSheetDefinitionStore characterSheets,
    IRulesPackageInstaller packages,
    ISshCredentialService credentials,
    IDiceRoller dice) : InstitutionBase(context), IAethericGm
{
    public ICampaignRepository Campaigns { get; } = campaigns;
    public ICharacterRepository Characters { get; } = characters;
    public IRulesCatalog Rules { get; } = rules;
    public ICharacterSheetDefinitionStore CharacterSheets { get; } = characterSheets;
    public IRulesPackageInstaller Packages { get; } = packages;
    public ISshCredentialService Credentials { get; } = credentials;
    public IDiceRoller Dice { get; } = dice;
}
