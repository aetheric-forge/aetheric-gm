using System.Text.Json;
namespace AethericGm.Core.Rules.Records;

public sealed record RulesRecord
{
    public RulesRecord(string key, string recordType, IReadOnlyDictionary<string, JsonElement> values)
    { Key = RulesKey.Require(key, nameof(key)); RecordType = RulesKey.Require(recordType, nameof(recordType)); ArgumentNullException.ThrowIfNull(values); Values = values.ToDictionary(pair => RulesKey.Require(pair.Key, nameof(values)), pair => pair.Value.Clone(), StringComparer.Ordinal); }
    public string Key { get; }
    public string RecordType { get; }
    public IReadOnlyDictionary<string, JsonElement> Values { get; }
}

public sealed record RulesRecordReference
{
    public RulesRecordReference(string recordType, string key) { RecordType = RulesKey.Require(recordType, nameof(recordType)); Key = RulesKey.Require(key, nameof(key)); }
    public string RecordType { get; }
    public string Key { get; }
}
