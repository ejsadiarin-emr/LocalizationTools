using System.Text.RegularExpressions;

namespace DataBank.Cli.Replacers;

public partial class RcReplacer : IFormatReplacer
{
    public string? ReplaceLine(string line, string oldValue, string newValue)
    {
        // Find the first quoted string in the line
        var match = QuotedStringPattern().Match(line);
        if (!match.Success)
            return null;

        var innerValue = match.Groups[1].Value;
        // Unescape RC double-quotes ("") → (") for comparison
        var unescapedInner = innerValue.Replace("\"\"", "\"");
        if (!string.Equals(unescapedInner, oldValue, StringComparison.Ordinal))
            return null;

        var replacement = $"\"{EscapeInternalQuotes(newValue)}\"";
        return line.Substring(0, match.Index) + replacement + line.Substring(match.Index + match.Length);
    }

    private static string EscapeInternalQuotes(string value)
    {
        return value.Replace("\"", "\"\"");
    }

    [GeneratedRegex(@"""((?:[^""]|"")*)""")]
    private static partial Regex QuotedStringPattern();
}
