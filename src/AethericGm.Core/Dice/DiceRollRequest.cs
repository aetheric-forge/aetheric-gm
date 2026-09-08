namespace AethericGm.Core.Dice;

public sealed record DiceRollContext(string Kind, string Id, string Label);

public sealed record DiceRollRequest
{
    public static readonly IReadOnlySet<int> SupportedSides = new HashSet<int> { 4, 6, 8, 10, 12, 20, 100 };

    public DiceRollRequest(int count, int sides, int modifier = 0, DiceRollMode mode = DiceRollMode.Normal, string? label = null, DiceRollContext? context = null, int? keepHighest = null)
    {
        if (count is < 1 or > 100) throw new ArgumentOutOfRangeException(nameof(count), "Roll between 1 and 100 dice.");
        if (!SupportedSides.Contains(sides)) throw new ArgumentOutOfRangeException(nameof(sides), "Use a d4, d6, d8, d10, d12, d20, or d100.");
        if (modifier is < -999 or > 999) throw new ArgumentOutOfRangeException(nameof(modifier), "Use a modifier between -999 and 999.");
        if (mode != DiceRollMode.Normal && (sides != 20 || count != 1)) throw new ArgumentException("Advantage and disadvantage require a single d20.", nameof(mode));
        if (keepHighest is not null && (mode != DiceRollMode.Normal || keepHighest < 1 || keepHighest >= count))
            throw new ArgumentOutOfRangeException(nameof(keepHighest), "Keep-highest must retain between one and one fewer than the number of dice in a normal roll.");
        Count = count; Sides = sides; Modifier = modifier; Mode = mode;
        Label = string.IsNullOrWhiteSpace(label) ? "Untitled roll" : label.Trim(); Context = context; KeepHighest = keepHighest;
    }

    public int Count { get; }
    public int Sides { get; }
    public int Modifier { get; }
    public DiceRollMode Mode { get; }
    public string Label { get; }
    public DiceRollContext? Context { get; }
    public int? KeepHighest { get; }
    public string Expression
    {
        get
        {
            var dice = Mode switch { DiceRollMode.Advantage => "2d20kh1", DiceRollMode.Disadvantage => "2d20kl1", _ => $"{Count}d{Sides}{(KeepHighest is null ? "" : $"kh{KeepHighest}")}" };
            return Modifier switch { > 0 => $"{dice} + {Modifier}", < 0 => $"{dice} - {Math.Abs(Modifier)}", _ => dice };
        }
    }
}
