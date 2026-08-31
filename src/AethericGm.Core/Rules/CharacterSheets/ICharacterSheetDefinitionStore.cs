namespace AethericGm.Core.Rules.CharacterSheets;

public interface ICharacterSheetDefinitionStore
{
    Task<CharacterSheetDefinition?> GetAsync(RulesetReference ruleset, CancellationToken cancellationToken = default);
    Task SaveAsync(CharacterSheetDefinition definition, CancellationToken cancellationToken = default);
}
