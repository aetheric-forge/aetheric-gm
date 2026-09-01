using AethericGm.Core.Dice;

namespace AethericGm.Tests;

public class InlineRulesTextParserTests
{
    [Theory]
    [InlineData("1d20", "1d20")]
    [InlineData("1d20+4", "1d20 + 4")]
    [InlineData("1d8-2", "1d8 - 2")]
    [InlineData("2d20kh1+3", "2d20kh1 + 3")]
    [InlineData("2d20kl1", "2d20kl1")]
    [InlineData("100d6+999", "100d6 + 999")]
    public void Supported_expressions_are_normalized(string input, string expected)
    {
        Assert.True(DiceExpressionParser.TryParse(input, "Test", null, out var request, out var error), error);
        Assert.Equal(expected, request!.Expression);
    }

    [Theory]
    [InlineData("1d20+(ability.wisdom)", "1d20 + (ability.wisdom)", "ability.wisdom", 3, 3)]
    [InlineData("1d20-(ability.strength)", "1d20 - (ability.strength)", "ability.strength", 2, -2)]
    [InlineData("2d20kh1+(ability.wisdom)", "2d20kh1 + (ability.wisdom)", "ability.wisdom", 4, 4)]
    public void Value_paths_are_preserved_and_resolved_explicitly(string input, string expected, string path, int value, int modifier)
    {
        Assert.True(DiceExpressionParser.TryParseTemplate(input, "Check", null, out var template, out var error), error);
        Assert.Equal(expected, template!.Expression); Assert.Equal(path, template.ModifierPath);
        Assert.False(template.TryResolve(null, out _));
        Assert.True(template.TryResolve(candidate => candidate == path ? value : null, out var request));
        Assert.Equal(modifier, request!.Modifier);
    }

    [Theory]
    [InlineData("d20")]
    [InlineData("1d7")]
    [InlineData("101d6")]
    [InlineData("2d6+1d4")]
    [InlineData("2d6kh1")]
    [InlineData("3d20kh1")]
    [InlineData("1d20+1000")]
    [InlineData("1d20+(Ability Wisdom)")]
    [InlineData("1d20+(ability)")]
    public void Unsupported_expressions_are_rejected(string input)
    {
        Assert.False(DiceExpressionParser.TryParse(input, "Test", null, out var request, out var error));
        Assert.Null(request); Assert.NotNull(error);
    }

    [Fact] public void Prose_is_split_without_changing_ordinary_text()
    {
        var context = new DiceRollContext("rules-record", "rules-neutral/1.0.0/ancestry/elf", "Elf");
        var segments = InlineRulesTextParser.Parse("Make a [roll:1d20+3|Strength check], then wait.", context);

        Assert.Equal(3, segments.Count); Assert.Equal("Make a ", segments[0].Text); Assert.Equal(", then wait.", segments[2].Text);
        var roll = Assert.IsType<DiceRollTemplate>(segments[1].Roll);
        Assert.Equal("Strength check", roll.Label); Assert.Same(context, roll.Context);
    }

    [Theory]
    [InlineData("Try [roll:1d20].")]
    [InlineData("Try [roll:1d20|].")]
    [InlineData("Try [roll:1d7|Odd die].")]
    [InlineData("Try [roll:1d20|Check|extra].")]
    [InlineData("Try [roll:1d20|Check")]
    public void Malformed_tokens_remain_text_and_produce_diagnostics(string text)
    {
        var segments = InlineRulesTextParser.Parse(text);
        Assert.Contains(segments, segment => segment.Error is not null && !segment.IsRoll);
        Assert.Single(InlineRulesTextParser.Diagnostics(text));
    }

    [Fact] public void Text_without_roll_tokens_is_untouched()
    {
        var text = "Ordinary [links] and prose stay ordinary.";
        var segment = Assert.Single(InlineRulesTextParser.Parse(text));
        Assert.Equal(text, segment.Text); Assert.False(segment.IsRoll); Assert.Null(segment.Error);
    }
}
