using System.Globalization;
using System.Text.RegularExpressions;

namespace AethericGm.Core.Dice;

public static partial class DiceExpressionParser
{
    public static bool TryParse(string expression, string? label, DiceRollContext? context, out DiceRollRequest? request, out string? error)
    {
        request = null;
        if (!TryParseTemplate(expression, label, context, out var template, out error)) return false;
        if (!template!.TryResolve(null, out request)) { error = $"{template.ModifierPath} requires a value context."; return false; }
        return true;
    }

    public static bool TryParseTemplate(string expression, string? label, DiceRollContext? context, out DiceRollTemplate? template, out string? error)
    {
        template = null; error = null;
        if (string.IsNullOrWhiteSpace(expression)) { error = "A dice expression is required."; return false; }
        var match = ExpressionPattern().Match(expression);
        if (!match.Success) { error = "Use NdX with an optional number or value path (for example, 1d20+3 or 1d20+(ability.wisdom))."; return false; }

        var count = int.Parse(match.Groups["count"].Value, CultureInfo.InvariantCulture);
        var sides = int.Parse(match.Groups["sides"].Value, CultureInfo.InvariantCulture);
        var keep = match.Groups["keep"].Value.ToLowerInvariant();
        var mode = keep switch { "kh1" => DiceRollMode.Advantage, "kl1" => DiceRollMode.Disadvantage, _ => DiceRollMode.Normal };
        if (keep.Length > 0 && (count != 2 || sides != 20)) { error = "kh1 and kl1 are supported only as 2d20kh1 or 2d20kl1."; return false; }

        var sign = match.Groups["sign"].Value == "-" ? -1 : 1;
        var amount = match.Groups["amount"].Success ? sign * int.Parse(match.Groups["amount"].Value, CultureInfo.InvariantCulture) : (int?)null;
        var reference = match.Groups["reference"].Success ? match.Groups["reference"].Value.ToLowerInvariant() : null;
        try
        {
            _ = new DiceRollRequest(mode == DiceRollMode.Normal ? count : 1, sides, amount ?? 0, mode, label, context);
            template = new DiceRollTemplate(count, sides, mode, amount, reference, sign, string.IsNullOrWhiteSpace(label) ? "Untitled roll" : label.Trim(), context);
            return true;
        }
        catch (ArgumentException exception) { error = exception.Message; return false; }
    }

    [GeneratedRegex(@"^\s*(?<count>[1-9]\d{0,2})d(?<sides>100|20|12|10|8|6|4)(?<keep>kh1|kl1)?(?:(?<sign>[+-])(?:(?<amount>\d{1,3})|\((?<reference>[a-z0-9]+(?:-[a-z0-9]+)*(?:\.[a-z0-9]+(?:-[a-z0-9]+)*)+)\)))?\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ExpressionPattern();
}
