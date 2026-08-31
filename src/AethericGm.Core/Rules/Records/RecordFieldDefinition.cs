namespace AethericGm.Core.Rules.Records;

public sealed record RecordFieldDefinition
{
    public RecordFieldDefinition(string key, string label, RecordValueKind valueKind, RecordCardinality cardinality = RecordCardinality.One, string? recordType = null, string? description = null)
    {
        Key = RulesKey.Require(key, nameof(key));
        Label = RulesKey.RequireLabel(label, nameof(label));
        ValueKind = valueKind;
        Cardinality = cardinality;
        RecordType = string.IsNullOrWhiteSpace(recordType) ? null : RulesKey.Require(recordType, nameof(recordType));
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        if (RequiresRecordType(valueKind) != (RecordType is not null))
            throw new ArgumentException(RequiresRecordType(valueKind) ? $"Field '{Key}' requires a record type." : $"Scalar field '{Key}' cannot name a record type.", nameof(recordType));
    }
    public string Key { get; }
    public string Label { get; }
    public RecordValueKind ValueKind { get; }
    public RecordCardinality Cardinality { get; }
    public string? RecordType { get; }
    public string? Description { get; }
    public static bool RequiresRecordType(RecordValueKind valueKind) => valueKind is RecordValueKind.Record or RecordValueKind.RulesReference or RecordValueKind.CharacterReference;
}
