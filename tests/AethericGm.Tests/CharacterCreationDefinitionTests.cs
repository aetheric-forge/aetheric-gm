using AethericGm.Core.Characters;
using AethericGm.Core.Rules;
using AethericGm.Core.Rules.Catalog;
using AethericGm.Core.Rules.CharacterSheets;
using AethericGm.Core.Rules.Records;

namespace AethericGm.Tests;

public sealed class CharacterCreationDefinitionTests
{
    private static readonly RulesetReference Reference = new("test", "1.0.0");

    [Fact]
    public void Creates_name_and_stable_ancestry_reference_without_copying_rules_content()
    {
        var creation = Build();
        var values = creation.CreateValues("  Ember Vale  ", new RulesRecordReference("ancestry", "elf"));
        Assert.Equal("Ember Vale", values["name"].GetString());
        Assert.Equal("ancestry", values["ancestry"].GetProperty("recordType").GetString());
        Assert.Equal("elf", values["ancestry"].GetProperty("key").GetString());
        Assert.Equal(2, values.Count);
    }

    [Fact]
    public void Rejects_missing_name_and_ancestry_outside_the_creation_catalog()
    {
        var creation = Build();
        Assert.Throws<ArgumentException>(() => creation.CreateValues(" ", new RulesRecordReference("ancestry", "elf")));
        Assert.Throws<ArgumentException>(() => creation.CreateValues("Ember", new RulesRecordReference("ancestry", "human")));
    }

    private static CharacterCreationDefinition Build()
    {
        var types = new RecordTypeRegistry([
            new RecordTypeDefinition("named", "Named", [new RecordFieldDefinition("name", "Name", RecordValueKind.Text)], displayField: "name"),
            new RecordTypeDefinition("ancestry", "Ancestry", [], extends: "named", displayField: "name")
        ]);
        var elf = new RulesRecord("elf", "ancestry", new Dictionary<string, System.Text.Json.JsonElement>
        {
            ["name"] = System.Text.Json.JsonSerializer.SerializeToElement("Elf")
        });
        var catalog = new RulesCatalogDefinition([new RulesCatalogSection("character-creation", "Character Creation",
            [new RulesCatalogItem("ancestries", "Ancestries", RulesCatalogItemKind.RecordCatalog, "ancestry")])]);
        var descriptor = new RulesetDescriptor(Reference, "Test", recordTypes: types, records: [elf], catalog: catalog);
        var sheet = new CharacterSheetDefinition(Reference, [new CharacterSheetSection("identity", "Identity", [
            new CharacterSheetField("name", "Name", RecordValueKind.Text),
            new CharacterSheetField("ancestry", "Ancestry", RecordValueKind.RulesReference, RecordCardinality.Optional, "ancestry")
        ])]);
        return new CharacterCreationDefinition(descriptor, sheet);
    }
}
