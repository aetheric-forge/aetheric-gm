using System.Text.Json;
using System.Text.Json.Serialization;
using AethericGm.Core.Rules;
using AethericGm.Core.Rules.Records;
using AethericGm.Core.Rules.Catalog;
namespace AethericGm.Infrastructure.Rules;
public sealed class FileRulesCatalog : IRulesCatalog
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.KebabCaseLower) }
    };
    private readonly IReadOnlyDictionary<RulesetReference, RulesetDescriptor> rulesets;
    public FileRulesCatalog(string rootPath)
    {
        if (string.IsNullOrWhiteSpace(rootPath)) throw new ArgumentException("Rules catalog path is required.", nameof(rootPath));
        if (!Directory.Exists(rootPath)) throw new DirectoryNotFoundException($"Rules catalog directory '{rootPath}' was not found.");
        var loaded = new Dictionary<RulesetReference, RulesetDescriptor>();
        foreach (var path in Directory.EnumerateFiles(rootPath, "manifest.json", SearchOption.AllDirectories).Order())
        {
            var manifest = JsonSerializer.Deserialize<Manifest>(File.ReadAllText(path), JsonOptions) ?? throw new InvalidDataException($"Ruleset manifest '{path}' is empty.");
            var packagePath = Path.GetDirectoryName(path)!;
            var types = LoadRecordTypes(packagePath);
            var records = LoadRecords(packagePath);
            ValidateRecords(types, records, path);
            var catalog = LoadCatalog(packagePath); catalog.ValidateAgainst(types);
            var descriptor = new RulesetDescriptor(new RulesetReference(manifest.Id, manifest.Version), manifest.Name, manifest.Description, types, records, catalog);
            if (!loaded.TryAdd(descriptor.Reference, descriptor)) throw new InvalidDataException($"Duplicate ruleset '{descriptor.Reference}' in '{path}'.");
        }
        rulesets = loaded;
    }

    private static RulesCatalogDefinition LoadCatalog(string packagePath)
    {
        var path = Path.Combine(packagePath, "catalog.json");
        if (!File.Exists(path)) return new RulesCatalogDefinition([]);
        var document = JsonSerializer.Deserialize<CatalogDocument>(File.ReadAllText(path), JsonOptions) ?? throw new InvalidDataException($"Rules catalog '{path}' is empty.");
        return new RulesCatalogDefinition(document.Sections.Select(section => new RulesCatalogSection(section.Key, section.Label, (section.Items ?? []).Select(ToItem))));
    }

    private static RulesCatalogItem ToItem(CatalogItemDocument item) =>
        new(item.Key, item.Label, item.Kind, item.RecordType, (item.Items ?? []).Select(ToItem));
    public IReadOnlyList<RulesetDescriptor> List() => rulesets.Values.OrderBy(value => value.Name, StringComparer.OrdinalIgnoreCase).ThenBy(value => value.Reference.Version, StringComparer.Ordinal).ToArray();
    public RulesetDescriptor? Resolve(RulesetReference reference) { ArgumentNullException.ThrowIfNull(reference); return rulesets.GetValueOrDefault(reference); }

    private static RecordTypeRegistry LoadRecordTypes(string packagePath)
    {
        var path = Path.Combine(packagePath, "record-types.json");
        if (!File.Exists(path)) return new RecordTypeRegistry([]);
        var document = JsonSerializer.Deserialize<RecordTypesDocument>(File.ReadAllText(path), JsonOptions) ?? throw new InvalidDataException($"Record types '{path}' are empty.");
        return new RecordTypeRegistry(document.RecordTypes.Select(type => new RecordTypeDefinition(type.Key, type.Label,
            type.Fields.Select(field => new RecordFieldDefinition(field.Key, field.Label, field.ValueKind, field.Cardinality, field.RecordType, field.Description, field.TextFormat)),
            type.Extends, type.DisplayField, type.Description)));
    }

    private static IReadOnlyList<RulesRecord> LoadRecords(string packagePath)
    {
        var path = Path.Combine(packagePath, "records.json");
        if (!File.Exists(path)) return [];
        var document = JsonSerializer.Deserialize<RecordsDocument>(File.ReadAllText(path), JsonOptions) ?? throw new InvalidDataException($"Rules records '{path}' are empty.");
        return document.Records.Select(record => new RulesRecord(record.Key, record.RecordType, record.Values)).ToArray();
    }

    private static void ValidateRecords(RecordTypeRegistry types, IReadOnlyList<RulesRecord> records, string manifestPath)
    {
        if (records.GroupBy(record => (record.RecordType, record.Key)).Any(group => group.Count() > 1))
            throw new InvalidDataException($"Ruleset '{manifestPath}' contains duplicate rules-record identities.");

        foreach (var record in records) ValidateRecordValues(types, record.RecordType, record.Values, $"{record.RecordType}/{record.Key}");
        foreach (var record in records)
            ValidateReferences(types, records, record.RecordType, record.Values, $"{record.RecordType}/{record.Key}");
    }

    private static void ValidateRecordValues(RecordTypeRegistry types, string recordType, IReadOnlyDictionary<string, JsonElement> values, string context)
    {
        if (types.Find(recordType) is null) throw new InvalidDataException($"Rules record '{context}' uses unknown type '{recordType}'.");
        var fields = types.FieldsFor(recordType);
        var known = fields.Select(field => field.Key).ToHashSet(StringComparer.Ordinal);
        var unknown = values.Keys.FirstOrDefault(key => !known.Contains(key));
        if (unknown is not null) throw new InvalidDataException($"Rules record '{context}' contains unknown field '{unknown}'.");

        foreach (var field in fields)
        {
            if (!values.TryGetValue(field.Key, out var value))
            {
                if (field.Cardinality is RecordCardinality.One or RecordCardinality.OneOrMore) throw new InvalidDataException($"Rules record '{context}' requires field '{field.Key}'.");
                continue;
            }

            if (field.Cardinality is RecordCardinality.Many or RecordCardinality.OneOrMore)
            {
                if (value.ValueKind != JsonValueKind.Array) throw new InvalidDataException($"Field '{context}.{field.Key}' must be an array.");
                if (field.Cardinality == RecordCardinality.OneOrMore && value.GetArrayLength() == 0) throw new InvalidDataException($"Field '{context}.{field.Key}' requires at least one value.");
                foreach (var item in value.EnumerateArray()) ValidateValue(types, field, item, $"{context}.{field.Key}");
            }
            else ValidateValue(types, field, value, $"{context}.{field.Key}");
        }
    }

    private static void ValidateValue(RecordTypeRegistry types, RecordFieldDefinition field, JsonElement value, string context)
    {
        var valid = field.ValueKind switch
        {
            RecordValueKind.Text => value.ValueKind == JsonValueKind.String,
            RecordValueKind.Integer => value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out _),
            RecordValueKind.Boolean => value.ValueKind is JsonValueKind.True or JsonValueKind.False,
            RecordValueKind.Record => value.ValueKind == JsonValueKind.Object,
            RecordValueKind.RulesReference => IsReference(value),
            RecordValueKind.CharacterReference => IsReference(value),
            _ => false
        };
        if (!valid) throw new InvalidDataException($"Field '{context}' is not a valid {field.ValueKind} value.");
        if (field.ValueKind == RecordValueKind.Record)
        {
            var properties = value.EnumerateObject().Select(property => property.Name).ToHashSet(StringComparer.Ordinal);
            if (!properties.SetEquals(["recordType", "values"])) throw new InvalidDataException($"Embedded record '{context}' must contain only recordType and values.");
            var actualType = value.GetProperty("recordType").GetString();
            if (actualType is null || !types.Accepts(field.RecordType!, actualType)) throw new InvalidDataException($"Embedded record '{context}' has incompatible type '{actualType}'.");
            var nested = value.GetProperty("values");
            if (nested.ValueKind != JsonValueKind.Object) throw new InvalidDataException($"Embedded record '{context}' values must be an object.");
            ValidateRecordValues(types, actualType, nested.EnumerateObject().ToDictionary(property => property.Name, property => property.Value), context);
        }
    }

    private static void ValidateReferences(RecordTypeRegistry types, IReadOnlyList<RulesRecord> records, string recordType, IReadOnlyDictionary<string, JsonElement> values, string context)
    {
        foreach (var field in types.FieldsFor(recordType))
        {
            if (!values.TryGetValue(field.Key, out var value)) continue;
            IEnumerable<JsonElement> items = field.Cardinality is RecordCardinality.Many or RecordCardinality.OneOrMore ? value.EnumerateArray().ToArray() : [value];
            foreach (var item in items)
            {
                if (field.ValueKind == RecordValueKind.RulesReference)
                {
                    var targetType = item.GetProperty("recordType").GetString()!;
                    var targetKey = item.GetProperty("key").GetString()!;
                    if (!types.Accepts(field.RecordType!, targetType) || records.All(record => record.RecordType != targetType || record.Key != targetKey))
                        throw new InvalidDataException($"Rules reference '{context}.{field.Key}' cannot resolve '{targetType}/{targetKey}'.");
                }
                else if (field.ValueKind == RecordValueKind.Record)
                {
                    var nestedType = item.GetProperty("recordType").GetString()!;
                    var nestedValues = item.GetProperty("values").EnumerateObject().ToDictionary(property => property.Name, property => property.Value);
                    ValidateReferences(types, records, nestedType, nestedValues, $"{context}.{field.Key}");
                }
            }
        }
    }

    private static bool IsReference(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Object) return false;
        var properties = value.EnumerateObject().ToArray();
        return properties.Length == 2 && properties.Select(property => property.Name).ToHashSet(StringComparer.Ordinal).SetEquals(["recordType", "key"])
            && value.GetProperty("recordType").ValueKind == JsonValueKind.String && value.GetProperty("key").ValueKind == JsonValueKind.String;
    }

    private sealed record Manifest(string Id, string Version, string Name, string? Description);
    private sealed record RecordTypesDocument(IReadOnlyList<RecordTypeDocument> RecordTypes);
    private sealed record RecordTypeDocument(string Key, string Label, string? Extends, string? DisplayField, string? Description, IReadOnlyList<RecordFieldDocument> Fields);
    private sealed record RecordFieldDocument(string Key, string Label, RecordValueKind ValueKind, RecordCardinality Cardinality, string? RecordType, string? Description, RecordTextFormat TextFormat = RecordTextFormat.PlainText);
    private sealed record RecordsDocument(IReadOnlyList<RulesRecordDocument> Records);
    private sealed record RulesRecordDocument(string Key, string RecordType, IReadOnlyDictionary<string, JsonElement> Values);
    private sealed record CatalogDocument(IReadOnlyList<CatalogSectionDocument> Sections);
    private sealed record CatalogSectionDocument(string Key, string Label, IReadOnlyList<CatalogItemDocument>? Items);
    private sealed record CatalogItemDocument(string Key, string Label, RulesCatalogItemKind Kind, string? RecordType, IReadOnlyList<CatalogItemDocument>? Items);
}
