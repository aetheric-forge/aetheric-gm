using System.Text.Json;
using AethericGm.Core.Characters;
using AethericGm.Core.Rules;
using AethericGm.Core.Rules.CharacterSheets;
using AethericGm.Core.Rules.Records;
using AethericGm.Web.Characters;

namespace AethericGm.Tests;

public sealed class CharacterSheetValueValidatorTests
{
    [Fact]
    public void Validates_nested_character_owned_records_and_rules_references()
    {
        var reference = new RulesetReference("test", "1.0.0");
        var types = new RecordTypeRegistry([
            new RecordTypeDefinition("ancestry", "Ancestry", []),
            new RecordTypeDefinition("attribute", "Attribute", [new RecordFieldDefinition("value", "Value", RecordValueKind.Integer)])
        ]);
        var elf = new RulesRecord("elf", "ancestry", new Dictionary<string, JsonElement>());
        var ruleset = new RulesetDescriptor(reference, "Test", recordTypes: types, records: [elf]);
        var sheet = new CharacterSheetDefinition(reference, [new CharacterSheetSection("main", "Main", [
            new CharacterSheetField("name", "Name", RecordValueKind.Text),
            new CharacterSheetField("ancestry", "Ancestry", RecordValueKind.RulesReference, RecordCardinality.Optional, "ancestry"),
            new CharacterSheetField("strength", "Strength", RecordValueKind.Record, RecordCardinality.Optional, "attribute")
        ])]);
        var valid = new Dictionary<string, JsonElement>
        {
            ["name"] = JsonSerializer.SerializeToElement("Ember"),
            ["ancestry"] = JsonSerializer.SerializeToElement(new RulesRecordReference("ancestry", "elf")),
            ["strength"] = JsonSerializer.SerializeToElement(new { recordType = "attribute", values = new { value = 14 } })
        };
        Assert.Empty(new CharacterSheetValueValidator(ruleset, sheet).Validate(valid));
        valid["ancestry"] = JsonSerializer.SerializeToElement(new RulesRecordReference("ancestry", "missing"));
        Assert.Contains(new CharacterSheetValueValidator(ruleset, sheet).Validate(valid), message => message.Contains("cannot resolve", StringComparison.Ordinal));
    }

    [Fact]
    public void Character_updates_values_without_changing_identity_or_ruleset()
    {
        var now = DateTimeOffset.UtcNow; var ruleset = new RulesetReference("test", "1.0.0");
        var character = Character.Create(Guid.NewGuid(), ruleset, new Dictionary<string, JsonElement> { ["name"] = JsonSerializer.SerializeToElement("Before") }, now);
        character.UpdateValues(new Dictionary<string, JsonElement> { ["name"] = JsonSerializer.SerializeToElement("After") }, now.AddMinutes(1));
        Assert.Equal("After", character.Name); Assert.Equal(ruleset, character.Ruleset); Assert.Equal(now.AddMinutes(1), character.UpdatedAt);
    }

    [Fact]
    public void Draft_round_trips_embedded_records_and_preserves_unknown_nested_values()
    {
        var reference = new RulesetReference("test", "1.0.0");
        var types = new RecordTypeRegistry([new RecordTypeDefinition("attribute", "Attribute", [
            new RecordFieldDefinition("value", "Value", RecordValueKind.Integer),
            new RecordFieldDefinition("bonus", "Bonus", RecordValueKind.Integer, RecordCardinality.Optional)
        ])]);
        var ruleset = new RulesetDescriptor(reference, "Test", recordTypes: types);
        var sheet = new CharacterSheetDefinition(reference, [new CharacterSheetSection("attributes", "Attributes", [
            new CharacterSheetField("strength", "Strength", RecordValueKind.Record, RecordCardinality.Optional, "attribute")
        ])]);
        var values = new Dictionary<string, JsonElement>
        {
            ["strength"] = JsonSerializer.SerializeToElement(new { recordType = "attribute", values = new { value = 14, legacy = "keep me" } })
        };
        var draft = new CharacterSheetDraft(sheet, ruleset, values);
        var rebuilt = draft.Build()["strength"];
        Assert.Equal(14, rebuilt.GetProperty("values").GetProperty("value").GetInt32());
        Assert.Equal("keep me", rebuilt.GetProperty("values").GetProperty("legacy").GetString());
    }
}
