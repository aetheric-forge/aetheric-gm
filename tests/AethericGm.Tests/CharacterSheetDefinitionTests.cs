using AethericGm.Core.Rules;
using AethericGm.Core.Rules.CharacterSheets;
using AethericGm.Infrastructure.Rules.CharacterSheets;

namespace AethericGm.Tests;

public sealed class CharacterSheetDefinitionTests
{
    private static readonly RulesetReference Shadowdark = new("shadowdark", "1.0.0");

    [Fact]
    public void Rejects_invalid_keys_duplicates_and_empty_choices()
    {
        Assert.Throws<ArgumentException>(() => new CharacterSheetField("Armor Class", "Armor Class", CharacterFieldType.Integer));
        Assert.Throws<ArgumentException>(() => new CharacterSheetField("class", "Class", CharacterFieldType.Choice));
        var field = new CharacterSheetField("name", "Name", CharacterFieldType.Text);
        Assert.Throws<ArgumentException>(() => new CharacterSheetSection("identity", "Identity", [field, field]));
    }

    [Fact]
    public async Task File_store_round_trips_a_definition()
    {
        var root = Path.Combine(Path.GetTempPath(), $"aetheric-sheet-{Guid.NewGuid():N}");
        try
        {
            var store = new FileCharacterSheetDefinitionStore(root);
            var definition = new CharacterSheetDefinition(Shadowdark,
            [
                new CharacterSheetSection("identity", "Identity",
                [
                    new CharacterSheetField("name", "Name", CharacterFieldType.Text, true),
                    new CharacterSheetField("alignment", "Alignment", CharacterFieldType.Choice, choices: ["Lawful", "Neutral", "Chaotic"])
                ])
            ]);
            await store.SaveAsync(definition);
            var loaded = await store.GetAsync(Shadowdark);
            Assert.NotNull(loaded); Assert.Equal("identity", Assert.Single(loaded.Sections).Key);
            Assert.Equal(2, loaded.Sections[0].Fields.Count); Assert.Equal(3, loaded.Sections[0].Fields[1].Choices.Count);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }
}
