namespace AethericGm.Core.Rules.Records;

public sealed record RecordTypeDefinition
{
    public RecordTypeDefinition(string key, string label, IEnumerable<RecordFieldDefinition> fields, string? extends = null, string? displayField = null, string? description = null)
    {
        Key = RulesKey.Require(key, nameof(key));
        Label = RulesKey.RequireLabel(label, nameof(label));
        Extends = string.IsNullOrWhiteSpace(extends) ? null : RulesKey.Require(extends, nameof(extends));
        DisplayField = string.IsNullOrWhiteSpace(displayField) ? null : RulesKey.Require(displayField, nameof(displayField));
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        Fields = (fields ?? throw new ArgumentNullException(nameof(fields))).ToArray();
        if (Fields.GroupBy(field => field.Key, StringComparer.Ordinal).Any(group => group.Count() > 1)) throw new ArgumentException($"Record type '{Key}' contains duplicate field keys.", nameof(fields));
    }
    public string Key { get; }
    public string Label { get; }
    public string? Extends { get; }
    public string? DisplayField { get; }
    public string? Description { get; }
    public IReadOnlyList<RecordFieldDefinition> Fields { get; }
}
