using System.Text.RegularExpressions;
namespace AethericGm.Core.Rules.Records;

internal static partial class RulesKey
{
    [GeneratedRegex("^[a-z][a-z0-9]*(?:-[a-z0-9]+)*$", RegexOptions.CultureInvariant)] private static partial Regex Pattern();
    public static string Require(string value, string parameter) { var normalized = RequireLabel(value, parameter); return Pattern().IsMatch(normalized) ? normalized : throw new ArgumentException("Keys must use lowercase kebab-case.", parameter); }
    public static string RequireLabel(string value, string parameter) => string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Value is required.", parameter) : value.Trim();
}
