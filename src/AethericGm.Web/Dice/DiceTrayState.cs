using AethericGm.Core.Dice;

namespace AethericGm.Web.Dice;

public sealed class DiceTrayState(IDiceRoller roller)
{
    private const int HistoryLimit = 20;
    private readonly List<DiceRollResult> history = [];
    public IReadOnlyList<DiceRollResult> History => history;
    public DiceRollResult? Selected { get; private set; }
    public DiceRollResult Roll(DiceRollRequest request)
    {
        var result = roller.Roll(request); history.Insert(0, result);
        if (history.Count > HistoryLimit) history.RemoveRange(HistoryLimit, history.Count - HistoryLimit);
        Selected = result;
        return result;
    }
    public void Select(DiceRollResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        if (!history.Contains(result)) throw new ArgumentException("Only a roll from this session can be selected.", nameof(result));
        Selected = result;
    }
}
