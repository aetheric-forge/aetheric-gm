using System.Text.Json;
using AethericGm.Core.Rules;
using AethericGm.Core.Rules.Catalog;
using AethericGm.Core.Rules.CharacterSheets;
using AethericGm.Core.Rules.Records;

namespace AethericGm.Core.Characters;

public sealed class CharacterCreationDefinition
{
    public const int DefaultAttributeMinimum = 3;
    public const int DefaultAttributeMaximum = 18;

    public CharacterCreationDefinition(RulesetDescriptor ruleset, CharacterSheetDefinition sheet)
    {
        Ruleset = ruleset ?? throw new ArgumentNullException(nameof(ruleset));
        Sheet = sheet ?? throw new ArgumentNullException(nameof(sheet));
        if (sheet.Ruleset != ruleset.Reference) throw new ArgumentException("Character sheet does not match the selected ruleset.", nameof(sheet));

        NameField = sheet.Sections.SelectMany(section => section.Fields).FirstOrDefault(field => field.Key == "name")
            ?? throw new InvalidDataException("The character-sheet definition does not provide a 'name' field.");
        if (NameField.ValueKind != RecordValueKind.Text || NameField.Cardinality != RecordCardinality.One)
            throw new InvalidDataException("The character-sheet 'name' field must be one required text value.");

        AncestryField = sheet.Sections.SelectMany(section => section.Fields).FirstOrDefault(field => field.Key == "ancestry")
            ?? throw new InvalidDataException("The character-sheet definition does not provide an 'ancestry' field.");
        if (AncestryField.ValueKind != RecordValueKind.RulesReference || AncestryField.Cardinality is RecordCardinality.Many or RecordCardinality.OneOrMore)
            throw new InvalidDataException("The character-sheet 'ancestry' field must be a single rules reference.");

        var section = ruleset.Catalog.Sections.FirstOrDefault(value => value.Key == "character-creation")
            ?? throw new InvalidDataException("The ruleset does not provide a Character Creation catalog.");
        var catalog = Flatten(section.Items).FirstOrDefault(item => item.Key == "ancestries" && item.Kind == RulesCatalogItemKind.RecordCatalog)
            ?? throw new InvalidDataException("The Character Creation catalog does not provide Ancestries.");
        if (!ruleset.RecordTypes.Accepts(AncestryField.RecordType!, catalog.RecordType!))
            throw new InvalidDataException("The Ancestries catalog is incompatible with the character-sheet ancestry field.");

        Ancestries = ruleset.RecordsOfType(catalog.RecordType!).ToArray();
        if (Ancestries.Count == 0) throw new InvalidDataException("The active ruleset does not currently provide ancestry choices.");

        AttributeFields = sheet.Sections.SelectMany(section => section.Fields)
            .Where(field => field.ValueKind == RecordValueKind.Record && field.RecordType is not null && ruleset.RecordTypes.Find("attribute") is not null &&
                ruleset.RecordTypes.Accepts("attribute", field.RecordType) && field.Cardinality is RecordCardinality.One or RecordCardinality.Optional)
            .Where(field => ruleset.RecordTypes.Find(field.RecordType!) is not null && ruleset.RecordTypes.FieldsFor(field.RecordType!).Any(IsAttributeValue))
            .ToArray();
        if (AttributeFields.Count is > 0 and not 6)
            throw new InvalidDataException("Character generation requires exactly six character-sheet fields backed by an attribute record.");
    }

    public RulesetDescriptor Ruleset { get; }
    public CharacterSheetDefinition Sheet { get; }
    public CharacterSheetField NameField { get; }
    public CharacterSheetField AncestryField { get; }
    public IReadOnlyList<RulesRecord> Ancestries { get; }
    public IReadOnlyList<CharacterSheetField> AttributeFields { get; }

    public IReadOnlyDictionary<string, JsonElement> CreateValues(string name, RulesRecordReference ancestry, IReadOnlyDictionary<string, int>? attributeScores = null)
    {
        var normalizedName = string.IsNullOrWhiteSpace(name) ? throw new ArgumentException("Character name is required.", nameof(name)) : name.Trim();
        ArgumentNullException.ThrowIfNull(ancestry);
        if (!Ruleset.RecordTypes.Accepts(AncestryField.RecordType!, ancestry.RecordType) ||
            Ancestries.All(record => record.RecordType != ancestry.RecordType || record.Key != ancestry.Key))
            throw new ArgumentException($"Ancestry '{ancestry.RecordType}/{ancestry.Key}' is not an available choice.", nameof(ancestry));

        if (AttributeFields.Count > 0)
        {
            if (attributeScores is null || attributeScores.Count != AttributeFields.Count || AttributeFields.Any(field => !attributeScores.ContainsKey(field.Key)))
                throw new ArgumentException("Assign one rolled score to each attribute.", nameof(attributeScores));
            if (attributeScores.Values.Any(score => score is < DefaultAttributeMinimum or > DefaultAttributeMaximum))
                throw new ArgumentOutOfRangeException(nameof(attributeScores), $"Attribute scores must be between {DefaultAttributeMinimum} and {DefaultAttributeMaximum}.");
        }

        var values = new Dictionary<string, JsonElement>(StringComparer.Ordinal)
        {
            [NameField.Key] = JsonSerializer.SerializeToElement(normalizedName),
            [AncestryField.Key] = JsonSerializer.SerializeToElement(ancestry)
        };
        foreach (var field in AttributeFields)
            values[field.Key] = JsonSerializer.SerializeToElement(new { recordType = field.RecordType, values = new { value = attributeScores![field.Key] } });
        return values;
    }

    public string Label(RulesRecord record)
    {
        var type = Ruleset.RecordTypes.Find(record.RecordType);
        return type?.DisplayField is { } field && record.Values.TryGetValue(field, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()!
            : record.Key;
    }

    private static IEnumerable<RulesCatalogItem> Flatten(IEnumerable<RulesCatalogItem> items) =>
        items.SelectMany(item => new[] { item }.Concat(Flatten(item.Items)));

    private static bool IsAttributeValue(RecordFieldDefinition field) =>
        field.Key == "value" && field.ValueKind == RecordValueKind.Integer && field.Cardinality == RecordCardinality.One;
}
