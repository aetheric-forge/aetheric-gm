using System.Security.Cryptography;

namespace AethericGm.Core.Dice;

public interface IDiceRandomSource { int Next(int inclusiveMinimum, int exclusiveMaximum); }
public sealed class CryptographicDiceRandomSource : IDiceRandomSource
{
    public int Next(int inclusiveMinimum, int exclusiveMaximum) => RandomNumberGenerator.GetInt32(inclusiveMinimum, exclusiveMaximum);
}
public interface IDiceRoller { DiceRollResult Roll(DiceRollRequest request); }

public sealed class DiceRoller(IDiceRandomSource random, TimeProvider timeProvider) : IDiceRoller
{
    public DiceRollResult Roll(DiceRollRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var rollCount = request.Mode == DiceRollMode.Normal ? request.Count : 2;
        var values = Enumerable.Range(0, rollCount).Select(_ => random.Next(1, request.Sides + 1)).ToArray();
        var selectedIndex = request.Mode switch
        {
            DiceRollMode.Advantage => Array.IndexOf(values, values.Max()),
            DiceRollMode.Disadvantage => Array.IndexOf(values, values.Min()),
            _ => -1
        };
        var keptIndexes = request.KeepHighest is null
            ? null
            : values.Select((value, index) => (value, index)).OrderByDescending(item => item.value).ThenBy(item => item.index)
                .Take(request.KeepHighest.Value).Select(item => item.index).ToHashSet();
        var dice = values.Select((value, index) => new DieResult(value, keptIndexes?.Contains(index) ?? (selectedIndex < 0 || index == selectedIndex))).ToArray();
        return new DiceRollResult(Guid.NewGuid(), request, dice, timeProvider.GetUtcNow());
    }
}
