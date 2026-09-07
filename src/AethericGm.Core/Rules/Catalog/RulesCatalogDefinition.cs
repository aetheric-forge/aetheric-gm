using AethericGm.Core.Rules.Records;

namespace AethericGm.Core.Rules.Catalog;

public sealed record RulesCatalogDefinition
{
    public RulesCatalogDefinition(IEnumerable<RulesCatalogSection> sections)
    {
        Sections = (sections ?? throw new ArgumentNullException(nameof(sections))).ToArray();
        RequireUnique(Sections.Select(section => section.Key), "catalog section");
    }

    public IReadOnlyList<RulesCatalogSection> Sections { get; }

    public void ValidateAgainst(RecordTypeRegistry recordTypes)
    {
        ArgumentNullException.ThrowIfNull(recordTypes);
        foreach (var section in Sections) ValidateItems(section.Items, recordTypes, section.Key);
    }

    private static void ValidateItems(IReadOnlyList<RulesCatalogItem> items, RecordTypeRegistry recordTypes, string path)
    {
        RequireUnique(items.Select(item => item.Key), $"catalog item below '{path}'");
        foreach (var item in items)
        {
            if (item.Kind == RulesCatalogItemKind.RecordCatalog && recordTypes.Find(item.RecordType!) is null)
                throw new InvalidDataException($"Record catalog '{path}/{item.Key}' targets unknown record type '{item.RecordType}'.");
            ValidateItems(item.Items, recordTypes, $"{path}/{item.Key}");
        }
    }

    private static void RequireUnique(IEnumerable<string> keys, string context)
    {
        if (keys.GroupBy(key => key, StringComparer.Ordinal).Any(group => group.Count() > 1))
            throw new InvalidDataException($"Duplicate {context} key.");
    }
}

public sealed record RulesCatalogSection
{
    public RulesCatalogSection(string key, string label, IEnumerable<RulesCatalogItem>? items = null)
    {
        Key = RulesKey.Require(key, nameof(key));
        Label = RulesKey.RequireLabel(label, nameof(label));
        Items = (items ?? []).ToArray();
    }

    public string Key { get; }
    public string Label { get; }
    public IReadOnlyList<RulesCatalogItem> Items { get; }
}

public sealed record RulesCatalogItem
{
    public RulesCatalogItem(string key, string label, RulesCatalogItemKind kind, string? recordType = null, IEnumerable<RulesCatalogItem>? items = null)
    {
        Key = RulesKey.Require(key, nameof(key));
        Label = RulesKey.RequireLabel(label, nameof(label));
        Kind = kind;
        RecordType = string.IsNullOrWhiteSpace(recordType) ? null : RulesKey.Require(recordType, nameof(recordType));
        Items = (items ?? []).ToArray();
        if (kind == RulesCatalogItemKind.RecordCatalog && RecordType is null) throw new ArgumentException($"Record catalog '{Key}' requires a record type.", nameof(recordType));
        if (kind == RulesCatalogItemKind.RecordCatalog && Items.Count > 0) throw new ArgumentException($"Record catalog '{Key}' cannot contain child items.", nameof(items));
        if (kind == RulesCatalogItemKind.Group && RecordType is not null) throw new ArgumentException($"Catalog group '{Key}' cannot name a record type.", nameof(recordType));
    }

    public string Key { get; }
    public string Label { get; }
    public RulesCatalogItemKind Kind { get; }
    public string? RecordType { get; }
    public IReadOnlyList<RulesCatalogItem> Items { get; }
}

public enum RulesCatalogItemKind { Group, RecordCatalog }
