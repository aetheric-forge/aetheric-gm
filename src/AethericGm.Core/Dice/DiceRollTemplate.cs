namespace AethericGm.Core.Dice;

public sealed record DiceRollTemplate(
    int Count,
    int Sides,
    DiceRollMode Mode,
    int? FixedModifier,
    string? ModifierPath,
    int ModifierSign,
    string Label,
    DiceRollContext? Context)
{
    public bool RequiresValue => ModifierPath is not null;
    public string Expression
    {
        get
        {
            var dice = Mode switch { DiceRollMode.Advantage => "2d20kh1", DiceRollMode.Disadvantage => "2d20kl1", _ => $"{Count}d{Sides}" };
            if (ModifierPath is not null) return $"{dice} {(ModifierSign < 0 ? "-" : "+")} ({ModifierPath})";
            return FixedModifier switch { > 0 => $"{dice} + {FixedModifier}", < 0 => $"{dice} - {Math.Abs(FixedModifier.Value)}", _ => dice };
        }
    }

    public bool TryResolve(Func<string, int?>? valueResolver, out DiceRollRequest? request)
    {
        request = null;
        var modifier = FixedModifier ?? 0;
        if (ModifierPath is not null)
        {
            var value = valueResolver?.Invoke(ModifierPath);
            if (value is null) return false;
            modifier = checked(ModifierSign * value.Value);
        }
        request = new DiceRollRequest(Mode == DiceRollMode.Normal ? Count : 1, Sides, modifier, Mode, Label, Context);
        return true;
    }
}
