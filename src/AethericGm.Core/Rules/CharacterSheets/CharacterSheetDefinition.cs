using System.Text.RegularExpressions;
using AethericGm.Core.Rules.Records;

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
    public void ValidateAgainst(RecordTypeRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);
        foreach (var field in Sections.SelectMany(section => section.Fields))
            if (field.RecordType is not null && registry.Find(field.RecordType) is null)
                throw new ArgumentException($"Character-sheet field '{field.Key}' targets unknown record type '{field.RecordType}'.", nameof(registry));
    }
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
    public CharacterSheetField(string key, string label, RecordValueKind valueKind, RecordCardinality cardinality = RecordCardinality.One, string? recordType = null)
    {
        Key = SheetKey.Require(key, nameof(key)); Label = SheetKey.RequireLabel(label, nameof(label)); ValueKind = valueKind; Cardinality = cardinality;
        RecordType = string.IsNullOrWhiteSpace(recordType) ? null : SheetKey.Require(recordType, nameof(recordType));
        if (RecordFieldDefinition.RequiresRecordType(valueKind) != (RecordType is not null))
            throw new ArgumentException(RecordFieldDefinition.RequiresRecordType(valueKind) ? $"Field '{Key}' requires a record type." : $"Scalar field '{Key}' cannot name a record type.", nameof(recordType));
    }
    public string Key { get; }
    public string Label { get; }
    public RecordValueKind ValueKind { get; }
    public RecordCardinality Cardinality { get; }
    public string? RecordType { get; }
}

internal static partial class SheetKey
{
    [GeneratedRegex("^[a-z][a-z0-9]*(?:-[a-z0-9]+)*$", RegexOptions.CultureInvariant)] private static partial Regex Pattern();
    public static string Require(string value, string parameter) { var normalized = RequireLabel(value, parameter); return Pattern().IsMatch(normalized) ? normalized : throw new ArgumentException("Keys must use lowercase kebab-case.", parameter); }
    public static string RequireLabel(string value, string parameter) => string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Value is required.", parameter) : value.Trim();
}
