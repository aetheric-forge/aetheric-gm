namespace AethericGm.Core.Dice;

public sealed record DieResult(int Value, bool IsKept = true);

public sealed record DiceRollResult(Guid Id, DiceRollRequest Request, IReadOnlyList<DieResult> Dice, DateTimeOffset RolledAt)
{
    public int Subtotal => Dice.Where(die => die.IsKept).Sum(die => die.Value);
    public int Total => Subtotal + Request.Modifier;
    public bool IsNaturalMaximum => Request.Sides == 20 && Dice.Any(die => die.IsKept && die.Value == 20);
    public bool IsNaturalMinimum => Request.Sides == 20 && Dice.Any(die => die.IsKept && die.Value == 1);
}
