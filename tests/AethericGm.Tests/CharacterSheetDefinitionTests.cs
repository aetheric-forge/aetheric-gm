using AethericGm.Core.Rules;
using AethericGm.Core.Rules.CharacterSheets;
using AethericGm.Core.Rules.Records;
using AethericGm.Infrastructure.Rules.CharacterSheets;

namespace AethericGm.Tests;

public sealed class CharacterSheetDefinitionTests
{
    private static readonly RulesetReference ExampleRules = new("example-fantasy", "1.0.0");

    [Fact]
    public void Rejects_invalid_keys_duplicates_and_missing_record_types()
    {
        Assert.Throws<ArgumentException>(() => new CharacterSheetField("Armor Class", "Armor Class", RecordValueKind.Integer));
        Assert.Throws<ArgumentException>(() => new CharacterSheetField("heritage", "Heritage", RecordValueKind.RulesReference));
        Assert.Throws<ArgumentException>(() => new CharacterSheetField("name", "Name", RecordValueKind.Text, recordType: "attribute"));
        var field = new CharacterSheetField("name", "Name", RecordValueKind.Text);
        Assert.Throws<ArgumentException>(() => new CharacterSheetSection("identity", "Identity", [field, field]));
    }

    [Fact]
    public void Validates_record_targets_against_a_registry()
    {
        var registry = new RecordTypeRegistry([new RecordTypeDefinition("attribute", "Attribute", [])]);
        var valid = new CharacterSheetDefinition(ExampleRules, [new CharacterSheetSection("attributes", "Attributes", [new CharacterSheetField("strength", "Strength", RecordValueKind.Record, RecordCardinality.Optional, "attribute")])]);
        valid.ValidateAgainst(registry);
        var invalid = new CharacterSheetDefinition(ExampleRules, [new CharacterSheetSection("identity", "Identity", [new CharacterSheetField("heritage", "Heritage", RecordValueKind.RulesReference, RecordCardinality.Optional, "heritage")])]);
        Assert.Throws<ArgumentException>(() => invalid.ValidateAgainst(registry));
    }

    [Fact]
    public async Task File_store_round_trips_a_definition()
    {
        var root = Path.Combine(Path.GetTempPath(), $"aetheric-sheet-{Guid.NewGuid():N}");
        try
        {
            var store = new FileCharacterSheetDefinitionStore(root);
            var definition = new CharacterSheetDefinition(ExampleRules,
            [
                new CharacterSheetSection("identity", "Identity",
                [
                    new CharacterSheetField("name", "Name", RecordValueKind.Text),
                    new CharacterSheetField("heritage", "Heritage", RecordValueKind.RulesReference, RecordCardinality.Optional, "heritage")
                ])
            ]);
            await store.SaveAsync(definition);
            var loaded = await store.GetAsync(ExampleRules);
            Assert.NotNull(loaded); Assert.Equal("identity", Assert.Single(loaded.Sections).Key);
            Assert.Equal(2, loaded.Sections[0].Fields.Count);
            Assert.Equal(RecordCardinality.Optional, loaded.Sections[0].Fields[1].Cardinality);
            Assert.Equal("heritage", loaded.Sections[0].Fields[1].RecordType);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Fact]
    public async Task File_store_round_trips_a_definition_in_an_installed_package_root()
    {
        var root = Path.Combine(Path.GetTempPath(), $"aetheric-package-sheet-{Guid.NewGuid():N}");
        try
        {
            var store = new FileCharacterSheetDefinitionStore(root, flatPackageLayout: true);
            var definition = new CharacterSheetDefinition(ExampleRules,
                [new CharacterSheetSection("identity", "Identity", [new CharacterSheetField("name", "Name", RecordValueKind.Text)])]);

            await store.SaveAsync(definition);

            Assert.True(File.Exists(Path.Combine(root, "character-sheet.json")));
            var loaded = await store.GetAsync(ExampleRules);
            Assert.Equal("name", Assert.Single(Assert.Single(loaded!.Sections).Fields).Key);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }
}
