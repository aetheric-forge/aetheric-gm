namespace AethericGm.Core.Rules.Records;

public sealed class RecordTypeRegistry
{
    private readonly IReadOnlyDictionary<string, RecordTypeDefinition> definitions;
    private readonly Dictionary<string, IReadOnlyList<RecordFieldDefinition>> effectiveFields = new(StringComparer.Ordinal);
    public RecordTypeRegistry(IEnumerable<RecordTypeDefinition> definitions)
    {
        ArgumentNullException.ThrowIfNull(definitions);
        var loaded = definitions.ToArray();
        if (loaded.GroupBy(definition => definition.Key, StringComparer.Ordinal).Any(group => group.Count() > 1)) throw new ArgumentException("Record-type registry contains duplicate keys.", nameof(definitions));
        this.definitions = loaded.ToDictionary(definition => definition.Key, StringComparer.Ordinal);
        foreach (var definition in loaded)
        {
            if (definition.Extends is not null && !this.definitions.ContainsKey(definition.Extends)) throw new ArgumentException($"Record type '{definition.Key}' extends unknown type '{definition.Extends}'.", nameof(definitions));
            foreach (var field in definition.Fields.Where(field => field.RecordType is not null))
                if (!this.definitions.ContainsKey(field.RecordType!)) throw new ArgumentException($"Field '{definition.Key}.{field.Key}' targets unknown record type '{field.RecordType}'.", nameof(definitions));
        }
        foreach (var definition in loaded)
        {
            var fields = ResolveFields(definition.Key, []);
            if (definition.DisplayField is not null)
            {
                var display = fields.SingleOrDefault(field => field.Key == definition.DisplayField) ?? throw new ArgumentException($"Record type '{definition.Key}' names unknown display field '{definition.DisplayField}'.", nameof(definitions));
                if (display.ValueKind != RecordValueKind.Text || display.Cardinality is RecordCardinality.Many or RecordCardinality.OneOrMore)
                    throw new ArgumentException($"Display field '{definition.Key}.{display.Key}' must be a singular text field.", nameof(definitions));
            }
        }
    }
    public IReadOnlyList<RecordTypeDefinition> List() => definitions.Values.OrderBy(value => value.Label, StringComparer.OrdinalIgnoreCase).ToArray();
    public RecordTypeDefinition? Find(string key) => string.IsNullOrWhiteSpace(key) ? null : definitions.GetValueOrDefault(key);
    public IReadOnlyList<RecordFieldDefinition> FieldsFor(string key) => effectiveFields.TryGetValue(key, out var fields) ? fields : throw new KeyNotFoundException($"Record type '{key}' is not registered.");
    public bool Accepts(string expectedType, string actualType)
    {
        if (!definitions.ContainsKey(expectedType) || !definitions.ContainsKey(actualType)) return false;
        for (var current = definitions[actualType]; ; current = definitions[current.Extends!])
        { if (current.Key == expectedType) return true; if (current.Extends is null) return false; }
    }
    private IReadOnlyList<RecordFieldDefinition> ResolveFields(string key, HashSet<string> path)
    {
        if (effectiveFields.TryGetValue(key, out var cached)) return cached;
        if (!path.Add(key)) throw new ArgumentException($"Record-type inheritance contains a cycle at '{key}'.", nameof(definitions));
        var definition = definitions[key];
        var fields = definition.Extends is null ? new List<RecordFieldDefinition>() : ResolveFields(definition.Extends, path).ToList();
        foreach (var field in definition.Fields)
        { if (fields.Any(existing => existing.Key == field.Key)) throw new ArgumentException($"Record type '{key}' replaces inherited field '{field.Key}'.", nameof(definitions)); fields.Add(field); }
        path.Remove(key);
        return effectiveFields[key] = fields.ToArray();
    }
}
