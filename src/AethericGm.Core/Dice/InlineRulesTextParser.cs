namespace AethericGm.Core.Dice;

public sealed record InlineRulesTextSegment(string Text, int Start, DiceRollTemplate? Roll = null, string? Error = null)
{
    public bool IsRoll => Roll is not null;
}

public static class InlineRulesTextParser
{
    private const string Opening = "[roll:";

    public static IReadOnlyList<InlineRulesTextSegment> Parse(string? text, DiceRollContext? context = null)
    {
        if (string.IsNullOrEmpty(text)) return [];
        var segments = new List<InlineRulesTextSegment>();
        var cursor = 0;
        while (cursor < text.Length)
        {
            var opening = text.IndexOf(Opening, cursor, StringComparison.OrdinalIgnoreCase);
            if (opening < 0) { segments.Add(new(text[cursor..], cursor)); break; }
            if (opening > cursor) segments.Add(new(text[cursor..opening], cursor));

            var closing = text.IndexOf(']', opening + Opening.Length);
            if (closing < 0)
            {
                segments.Add(new(text[opening..], opening, Error: "Roll token is missing its closing ]."));
                break;
            }

            var token = text[opening..(closing + 1)];
            var body = text[(opening + Opening.Length)..closing];
            var divider = body.IndexOf('|');
            if (divider <= 0 || divider == body.Length - 1)
            {
                segments.Add(new(token, opening, Error: "Use [roll:expression|label] with both an expression and label."));
            }
            else
            {
                var expression = body[..divider].Trim();
                var label = body[(divider + 1)..].Trim();
                if (body[(divider + 1)..].Contains('|'))
                    segments.Add(new(token, opening, Error: "A roll label cannot contain |."));
                else if (DiceExpressionParser.TryParseTemplate(expression, label, context, out var template, out var error))
                    segments.Add(new(token, opening, template));
                else
                    segments.Add(new(token, opening, Error: error));
            }
            cursor = closing + 1;
        }
        return segments;
    }

    public static IReadOnlyList<string> Diagnostics(string? text) => Parse(text)
        .Where(segment => segment.Error is not null)
        .Select(segment => $"Character {segment.Start + 1}: {segment.Error}")
        .ToArray();
}
