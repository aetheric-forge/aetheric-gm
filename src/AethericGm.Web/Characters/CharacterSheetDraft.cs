using System.Text.Json;
using AethericGm.Core.Rules;
using AethericGm.Core.Rules.CharacterSheets;
using AethericGm.Core.Rules.Records;

namespace AethericGm.Web.Characters;

public sealed class CharacterSheetDraft
{
    private readonly IReadOnlyDictionary<string, JsonElement> original;
    private readonly HashSet<string> knownKeys;

    public CharacterSheetDraft(CharacterSheetDefinition definition, RulesetDescriptor ruleset, IReadOnlyDictionary<string, JsonElement> values)
    {
        original = values;
        knownKeys = definition.Sections.SelectMany(section => section.Fields).Select(field => field.Key).ToHashSet(StringComparer.Ordinal);
        Sections = definition.Sections.Select(section => new CharacterSectionDraft(section.Label,
            section.Fields.Select(field => CharacterFieldDraft.Create(field.Key, field.Label, field.ValueKind, field.Cardinality, field.RecordType,
                values.GetValueOrDefault(field.Key), ruleset)).ToArray())).ToArray();
    }

    public IReadOnlyList<CharacterSectionDraft> Sections { get; }

    public IReadOnlyDictionary<string, JsonElement> Build()
    {
        var result = original.Where(pair => !knownKeys.Contains(pair.Key)).ToDictionary(pair => pair.Key, pair => pair.Value.Clone(), StringComparer.Ordinal);
        foreach (var field in Sections.SelectMany(section => section.Fields))
            if (field.Build() is { } value) result[field.Key] = value;
        return result;
    }
}

public sealed record CharacterSectionDraft(string Label, IReadOnlyList<CharacterFieldDraft> Fields);

public sealed class CharacterFieldDraft
{
    private CharacterFieldDraft(string key, string label, RecordValueKind valueKind, RecordCardinality cardinality, string? recordType, RulesetDescriptor ruleset)
    { Key = key; Label = label; ValueKind = valueKind; Cardinality = cardinality; RecordType = recordType; Ruleset = ruleset; }

    public string Key { get; }
    public string Label { get; }
    public RecordValueKind ValueKind { get; }
    public RecordCardinality Cardinality { get; }
    public string? RecordType { get; }
    public RulesetDescriptor Ruleset { get; }
    public List<CharacterValueDraft> Values { get; } = [];
    public bool IsMany => Cardinality is RecordCardinality.Many or RecordCardinality.OneOrMore;
    public bool IsOptional => Cardinality is RecordCardinality.Optional or RecordCardinality.Many;

    public static CharacterFieldDraft Create(string key, string label, RecordValueKind valueKind, RecordCardinality cardinality, string? recordType,
        JsonElement value, RulesetDescriptor ruleset)
    {
        var draft = new CharacterFieldDraft(key, label, valueKind, cardinality, recordType, ruleset);
        if (value.ValueKind != JsonValueKind.Undefined)
        {
            IEnumerable<JsonElement> items = draft.IsMany && value.ValueKind == JsonValueKind.Array ? value.EnumerateArray().ToArray() : [value];
            foreach (var item in items) draft.Values.Add(CharacterValueDraft.From(draft, item));
        }
        else if (!draft.IsOptional) draft.Add();
        return draft;
    }

    public void Add() { if (!IsMany) Values.Clear(); Values.Add(CharacterValueDraft.Empty(this)); }
    public void Remove(CharacterValueDraft value) => Values.Remove(value);

    public JsonElement? Build()
    {
        if (Values.Count == 0) return null;
        var built = Values.Select(value => value.Build(this)).ToArray();
        return IsMany ? JsonSerializer.SerializeToElement(built) : built[0];
    }

    public IReadOnlyList<RulesRecord> ReferenceChoices() => RecordType is null ? [] : Ruleset.RecordsOfType(RecordType);
    public string LabelFor(RulesRecord record)
    {
        var display = Ruleset.RecordTypes.Find(record.RecordType)?.DisplayField;
        return display is not null && record.Values.TryGetValue(display, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString()! : record.Key;
    }
}

public sealed class CharacterValueDraft
{
    private Dictionary<string, JsonElement> originalRecordValues = new(StringComparer.Ordinal);
    private HashSet<string> knownRecordKeys = new(StringComparer.Ordinal);
    public string Scalar { get; set; } = "";
    public string Reference { get; set; } = "";
    public string RecordType { get; private set; } = "";
    public List<CharacterFieldDraft> Fields { get; } = [];

    public static CharacterValueDraft Empty(CharacterFieldDraft field)
    {
        var result = new CharacterValueDraft();
        if (field.ValueKind == RecordValueKind.Record) result.LoadRecord(field, field.RecordType!, default);
        return result;
    }

    public static CharacterValueDraft From(CharacterFieldDraft field, JsonElement value)
    {
        var result = new CharacterValueDraft();
        switch (field.ValueKind)
        {
            case RecordValueKind.Text: result.Scalar = value.ValueKind == JsonValueKind.String ? value.GetString() ?? "" : value.GetRawText(); break;
            case RecordValueKind.Integer or RecordValueKind.Boolean: result.Scalar = value.GetRawText(); break;
            case RecordValueKind.RulesReference or RecordValueKind.CharacterReference:
                if (value.ValueKind == JsonValueKind.Object && value.TryGetProperty("recordType", out var type) && value.TryGetProperty("key", out var key)) result.Reference = $"{type.GetString()}/{key.GetString()}";
                break;
            case RecordValueKind.Record:
                var actualType = value.ValueKind == JsonValueKind.Object && value.TryGetProperty("recordType", out var recordType) ? recordType.GetString() : field.RecordType;
                result.LoadRecord(field, actualType ?? field.RecordType!, value);
                break;
        }
        return result;
    }

    private void LoadRecord(CharacterFieldDraft parent, string actualType, JsonElement value)
    {
        RecordType = actualType;
        var nested = value.ValueKind == JsonValueKind.Object && value.TryGetProperty("values", out var values) && values.ValueKind == JsonValueKind.Object
            ? values.EnumerateObject().ToDictionary(property => property.Name, property => property.Value, StringComparer.Ordinal)
            : new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        originalRecordValues = nested;
        var definitions = parent.Ruleset.RecordTypes.FieldsFor(actualType);
        knownRecordKeys = definitions.Select(field => field.Key).ToHashSet(StringComparer.Ordinal);
        foreach (var field in definitions)
            Fields.Add(CharacterFieldDraft.Create(field.Key, field.Label, field.ValueKind, field.Cardinality, field.RecordType, nested.GetValueOrDefault(field.Key), parent.Ruleset));
    }

    public JsonElement Build(CharacterFieldDraft field) => field.ValueKind switch
    {
        RecordValueKind.Text => JsonSerializer.SerializeToElement(Scalar),
        RecordValueKind.Integer => JsonSerializer.SerializeToElement(long.TryParse(Scalar, out var number) ? number : throw new ArgumentException($"{field.Label} must be an integer.")),
        RecordValueKind.Boolean => JsonSerializer.SerializeToElement(bool.TryParse(Scalar, out var boolean) ? boolean : throw new ArgumentException($"{field.Label} must be Yes or No.")),
        RecordValueKind.RulesReference or RecordValueKind.CharacterReference => BuildReference(field),
        RecordValueKind.Record => BuildRecord(),
        _ => throw new ArgumentOutOfRangeException()
    };

    private JsonElement BuildReference(CharacterFieldDraft field)
    {
        var parts = Reference.Split('/', 2);
        if (parts.Length != 2) throw new ArgumentException($"{field.Label} requires a selection.");
        return JsonSerializer.SerializeToElement(new RulesRecordReference(parts[0], parts[1]));
    }

    private JsonElement BuildRecord()
    {
        var values = originalRecordValues.Where(pair => !knownRecordKeys.Contains(pair.Key)).ToDictionary(pair => pair.Key, pair => pair.Value.Clone(), StringComparer.Ordinal);
        foreach (var field in Fields) if (field.Build() is { } value) values[field.Key] = value;
        return JsonSerializer.SerializeToElement(new { recordType = RecordType, values });
    }
}
