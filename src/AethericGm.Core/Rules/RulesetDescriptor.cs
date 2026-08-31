using AethericGm.Core.Rules.Records;
namespace AethericGm.Core.Rules;
public sealed record RulesetDescriptor
{
    public RulesetDescriptor(RulesetReference reference, string name, string? description = null, RecordTypeRegistry? recordTypes = null, IEnumerable<RulesRecord>? records = null)
    {
        Reference = reference ?? throw new ArgumentNullException(nameof(reference));
        Name = string.IsNullOrWhiteSpace(name) ? throw new ArgumentException("Ruleset name is required.", nameof(name)) : name.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        RecordTypes = recordTypes ?? new RecordTypeRegistry([]);
        Records = (records ?? []).ToArray();
    }
    public RulesetReference Reference { get; }
    public string Name { get; }
    public string? Description { get; }
    public RecordTypeRegistry RecordTypes { get; }
    public IReadOnlyList<RulesRecord> Records { get; }
    public IReadOnlyList<RulesRecord> RecordsOfType(string recordType) => Records.Where(record => RecordTypes.Accepts(recordType, record.RecordType)).ToArray();
}
