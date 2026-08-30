namespace AethericGm.Core.Rules;
public interface IRulesCatalog { IReadOnlyList<RulesetDescriptor> List(); RulesetDescriptor? Resolve(RulesetReference reference); }
