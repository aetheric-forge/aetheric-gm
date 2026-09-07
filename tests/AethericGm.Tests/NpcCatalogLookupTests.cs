using System.Text.Json;
using AethericGm.Core.Npcs;
using AethericGm.Core.Rules;
using AethericGm.Core.Rules.Catalog;
using AethericGm.Core.Rules.Records;

namespace AethericGm.Tests;

public sealed class NpcCatalogLookupTests
{
    private static readonly RulesetReference Reference = new("test", "1.0.0");

    [Fact]
    public void Finds_records_published_through_the_npcs_catalog_convention()
    {
        var types = new RecordTypeRegistry([new RecordTypeDefinition("npc", "NPC", [new RecordFieldDefinition("name", "Name", RecordValueKind.Text)], displayField: "name")]);
        var goblin = new RulesRecord("goblin", "npc", new Dictionary<string, JsonElement> { ["name"] = JsonSerializer.SerializeToElement("Goblin") });
        var catalog = new RulesCatalogDefinition([new RulesCatalogSection("npcs", "NPCs", [new RulesCatalogItem("npcs", "NPCs", RulesCatalogItemKind.RecordCatalog, "npc")])]);
        var ruleset = new RulesetDescriptor(Reference, "Test", recordTypes: types, records: [goblin], catalog: catalog);

        var records = NpcCatalogLookup.CompatibleRecords(ruleset);

        Assert.Equal("goblin", Assert.Single(records).Key);
    }

    [Fact]
    public void Returns_empty_when_the_ruleset_publishes_no_npc_catalog()
    {
        var ruleset = new RulesetDescriptor(Reference, "Test");
        Assert.Empty(NpcCatalogLookup.CompatibleRecords(ruleset));
    }
}
