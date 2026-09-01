using AethericGm.Core.Dice;
using AethericGm.Web.Dice;

namespace AethericGm.Tests;

public class DiceRollerTests
{
    [Fact] public void Normal_roll_records_each_die_and_applies_modifier()
    {
        var result = CreateRoller(3, 5).Roll(new DiceRollRequest(2, 6, 4, label: "Damage"));
        Assert.Equal([3, 5], result.Dice.Select(die => die.Value)); Assert.All(result.Dice, die => Assert.True(die.IsKept));
        Assert.Equal(8, result.Subtotal); Assert.Equal(12, result.Total); Assert.Equal("2d6 + 4", result.Request.Expression); Assert.Equal("Damage", result.Request.Label);
    }
    [Fact] public void Advantage_keeps_the_highest_d20_before_applying_modifier()
    {
        var result = CreateRoller(7, 18).Roll(new DiceRollRequest(1, 20, 3, DiceRollMode.Advantage));
        Assert.Equal([false, true], result.Dice.Select(die => die.IsKept)); Assert.Equal(18, result.Subtotal); Assert.Equal(21, result.Total); Assert.Equal("2d20kh1 + 3", result.Request.Expression);
    }
    [Fact] public void Disadvantage_keeps_the_lowest_d20_and_detects_natural_one()
    {
        var result = CreateRoller(14, 1).Roll(new DiceRollRequest(1, 20, -2, DiceRollMode.Disadvantage));
        Assert.Equal([false, true], result.Dice.Select(die => die.IsKept)); Assert.Equal(-1, result.Total); Assert.True(result.IsNaturalMinimum); Assert.Equal("2d20kl1 - 2", result.Request.Expression);
    }
    [Fact] public void Natural_twenty_is_based_on_kept_die_not_modified_total()
    {
        var result = CreateRoller(20).Roll(new DiceRollRequest(1, 20, -10)); Assert.True(result.IsNaturalMaximum); Assert.Equal(10, result.Total);
    }
    [Fact] public void Request_rejects_unsupported_dice_and_invalid_advantage()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new DiceRollRequest(1, 7));
        Assert.Throws<ArgumentException>(() => new DiceRollRequest(2, 20, mode: DiceRollMode.Advantage));
        Assert.Throws<ArgumentException>(() => new DiceRollRequest(1, 12, mode: DiceRollMode.Disadvantage));
    }
    [Fact] public void Context_is_retained_for_future_character_and_npc_rolls()
    {
        var context = new DiceRollContext("character", "ember", "Ember Vale");
        var result = CreateRoller(11).Roll(new DiceRollRequest(1, 20, 2, label: "Strength check", context: context));
        Assert.Same(context, result.Request.Context); Assert.Equal("Strength check", result.Request.Label);
    }
    [Fact] public void Tray_keeps_the_twenty_most_recent_rolls_and_can_repeat_a_request()
    {
        var source = new ConstantRandom(4);
        var tray = new DiceTrayState(new DiceRoller(source, new FixedTimeProvider(DateTimeOffset.Parse("2026-08-31T12:00:00Z"))));
        for (var index = 1; index <= 21; index++) tray.Roll(new DiceRollRequest(1, 6, label: $"Roll {index}"));

        Assert.Equal(20, tray.History.Count); Assert.Equal("Roll 21", tray.History[0].Request.Label); Assert.Equal("Roll 2", tray.History[^1].Request.Label);
        Assert.Same(tray.History[0], tray.Selected);
        tray.Select(tray.History[5]); Assert.Same(tray.History[5], tray.Selected);
        var repeated = tray.Roll(tray.History[0].Request);
        Assert.Equal("Roll 21", repeated.Request.Label); Assert.Equal(20, tray.History.Count); Assert.Same(repeated, tray.Selected);
    }
    private static DiceRoller CreateRoller(params int[] values) => new(new SequenceRandom(values), new FixedTimeProvider(DateTimeOffset.Parse("2026-08-31T12:00:00Z")));
    private sealed class SequenceRandom(params int[] values) : IDiceRandomSource
    {
        private int index;
        public int Next(int inclusiveMinimum, int exclusiveMaximum) { var value = values[index++]; Assert.InRange(value, inclusiveMinimum, exclusiveMaximum - 1); return value; }
    }
    private sealed class ConstantRandom(int value) : IDiceRandomSource { public int Next(int inclusiveMinimum, int exclusiveMaximum) => value; }
    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider { public override DateTimeOffset GetUtcNow() => now; }
}
