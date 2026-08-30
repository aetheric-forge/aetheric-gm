using System.Text.RegularExpressions;
namespace AethericGm.Core.Rules;
public sealed record RulesetReference
{
    private static readonly Regex IdPattern = new("^[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.CultureInvariant);
    private static readonly Regex VersionPattern = new("^[0-9]+\\.[0-9]+\\.[0-9]+(?:-[0-9A-Za-z.-]+)?$", RegexOptions.CultureInvariant);
    public RulesetReference(string id, string version) { Id = Normalize(id, nameof(id), IdPattern, "lowercase kebab-case"); Version = Normalize(version, nameof(version), VersionPattern, "semantic version format"); }
    public string Id { get; }
    public string Version { get; }
    public override string ToString() => $"{Id}@{Version}";
    private static string Normalize(string value, string parameter, Regex pattern, string expected) { if (string.IsNullOrWhiteSpace(value) || !pattern.IsMatch(value.Trim())) throw new ArgumentException($"Ruleset {parameter} must use {expected}.", parameter); return value.Trim(); }
}
