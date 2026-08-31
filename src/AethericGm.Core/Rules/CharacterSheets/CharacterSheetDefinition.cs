using System.Text.RegularExpressions;

namespace AethericGm.Core.Rules.CharacterSheets;

public sealed record CharacterSheetDefinition
{
    public CharacterSheetDefinition(RulesetReference ruleset, IEnumerable<CharacterSheetSection> sections)
    {
        Ruleset = ruleset ?? throw new ArgumentNullException(nameof(ruleset));
        Sections = (sections ?? throw new ArgumentNullException(nameof(sections))).ToArray();
        EnsureUnique(Sections.Select(section => section.Key), "section");
    }
    public RulesetReference Ruleset { get; }
    public IReadOnlyList<CharacterSheetSection> Sections { get; }
    private static void EnsureUnique(IEnumerable<string> keys, string kind)
    { if (keys.GroupBy(key => key, StringComparer.Ordinal).Any(group => group.Count() > 1)) throw new ArgumentException($"Character sheet contains duplicate {kind} keys."); }
}

public sealed record CharacterSheetSection
{
    public CharacterSheetSection(string key, string label, IEnumerable<CharacterSheetField> fields)
    {
        Key = SheetKey.Require(key, nameof(key)); Label = SheetKey.RequireLabel(label, nameof(label));
        Fields = (fields ?? throw new ArgumentNullException(nameof(fields))).ToArray();
        if (Fields.GroupBy(field => field.Key, StringComparer.Ordinal).Any(group => group.Count() > 1)) throw new ArgumentException($"Section '{Key}' contains duplicate field keys.");
    }
    public string Key { get; }
    public string Label { get; }
    public IReadOnlyList<CharacterSheetField> Fields { get; }
}

public sealed record CharacterSheetField
{
    public CharacterSheetField(string key, string label, CharacterFieldType type, bool required = false, IEnumerable<string>? choices = null)
    {
        Key = SheetKey.Require(key, nameof(key)); Label = SheetKey.RequireLabel(label, nameof(label)); Type = type; Required = required;
        Choices = choices?.Select(value => SheetKey.RequireLabel(value, nameof(choices))).Distinct(StringComparer.Ordinal).ToArray() ?? [];
        if (Type == CharacterFieldType.Choice && Choices.Count == 0) throw new ArgumentException($"Choice field '{Key}' requires at least one choice.", nameof(choices));
        if (Type != CharacterFieldType.Choice && Choices.Count > 0) throw new ArgumentException($"Only choice fields may define choices.", nameof(choices));
    }
    public string Key { get; }
    public string Label { get; }
    public CharacterFieldType Type { get; }
    public bool Required { get; }
    public IReadOnlyList<string> Choices { get; }
}

internal static partial class SheetKey
{
    [GeneratedRegex("^[a-z][a-z0-9]*(?:-[a-z0-9]+)*$", RegexOptions.CultureInvariant)] private static partial Regex Pattern();
    public static string Require(string value, string parameter) { var normalized = RequireLabel(value, parameter); return Pattern().IsMatch(normalized) ? normalized : throw new ArgumentException("Keys must use lowercase kebab-case.", parameter); }
    public static string RequireLabel(string value, string parameter) => string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Value is required.", parameter) : value.Trim();
}
