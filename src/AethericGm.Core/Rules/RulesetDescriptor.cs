namespace AethericGm.Core.Rules;
public sealed record RulesetDescriptor
{
    public RulesetDescriptor(RulesetReference reference, string name, string? description = null) { Reference = reference ?? throw new ArgumentNullException(nameof(reference)); Name = string.IsNullOrWhiteSpace(name) ? throw new ArgumentException("Ruleset name is required.", nameof(name)) : name.Trim(); Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(); }
    public RulesetReference Reference { get; }
    public string Name { get; }
    public string? Description { get; }
}
