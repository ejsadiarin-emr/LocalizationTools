using System.Net;
using System.Text.RegularExpressions;

namespace DataBank.Cli.Replacers;

public partial class ResxReplacer : IFormatReplacer
{
    public string? ReplaceLine(string line, string oldValue, string newValue)
    {
        var match = ValuePattern().Match(line);
        if (!match.Success)
            return null;

        var currentValue = match.Groups[1].Value;

        // Decode XML entities for comparison
        var decodedOld = WebUtility.HtmlDecode(currentValue);
        if (!string.Equals(decodedOld, oldValue, StringComparison.Ordinal))
            return null;

        // Encode new value with XML entities
        var escapedNew = WebUtility.HtmlEncode(newValue);

        // Replace in the line
        var valueStart = match.Groups[1].Index;
        var valueLength = match.Groups[1].Length;

        return line.Substring(0, valueStart) + escapedNew + line.Substring(valueStart + valueLength);
    }

    [GeneratedRegex(@"<value>([^<]*)</value>")]
    private static partial Regex ValuePattern();
}
