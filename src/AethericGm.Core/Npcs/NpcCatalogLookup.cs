using AethericGm.Core.Rules;
using AethericGm.Core.Rules.Catalog;
using AethericGm.Core.Rules.Records;

namespace AethericGm.Core.Npcs;

// Mirrors how CharacterCreationDefinition locates ancestries: catalog section "npcs" -> record-catalog item "npcs" names the ruleset's NPC record type without the app assuming it.
public static class NpcCatalogLookup
{
    public static IReadOnlyList<RulesRecord> CompatibleRecords(RulesetDescriptor ruleset)
    {
        ArgumentNullException.ThrowIfNull(ruleset);
        var section = ruleset.Catalog.Sections.FirstOrDefault(value => value.Key == "npcs");
        var item = section is null
            ? null
            : Flatten(section.Items).FirstOrDefault(value => value.Key == "npcs" && value.Kind == RulesCatalogItemKind.RecordCatalog);
        return item is null ? [] : ruleset.RecordsOfType(item.RecordType!).ToArray();
    }

    private static IEnumerable<RulesCatalogItem> Flatten(IEnumerable<RulesCatalogItem> items) =>
        items.SelectMany(item => new[] { item }.Concat(Flatten(item.Items)));
}
