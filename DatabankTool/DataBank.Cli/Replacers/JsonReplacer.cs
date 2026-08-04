using System.Text.RegularExpressions;

namespace DataBank.Cli.Replacers;

public partial class JsonReplacer : IFormatReplacer
{
    public string? ReplaceLine(string line, string oldValue, string newValue)
    {
        var match = JsonEntryPattern().Match(line);
        if (!match.Success)
            return null;

        var currentValue = match.Groups[2].Value;

        if (!string.Equals(currentValue, oldValue, StringComparison.Ordinal))
            return null;

        // Escape new value for JSON: " → \", \ → \\, \r → \r, \n → \n
        var escapedNew = newValue.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n");

        var valueStart = match.Groups[2].Index;
        var valueLength = match.Groups[2].Length;

        return line.Substring(0, valueStart) + escapedNew + line.Substring(valueStart + valueLength);
    }

    [GeneratedRegex(@"^\s*""([^""]*)""\s*:\s*""([^""]*)""")]
    private static partial Regex JsonEntryPattern();
}
