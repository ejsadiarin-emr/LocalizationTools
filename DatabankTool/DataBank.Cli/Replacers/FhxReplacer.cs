namespace DataBank.Cli.Replacers;

public class FhxReplacer : IFormatReplacer
{
    public string? ReplaceLine(string line, string oldValue, string newValue)
    {
        var parts = line.Split('\t');
        if (parts.Length < 3)
            return null;

        // Value is everything after the second tab
        var currentValue = string.Join("\t", parts.Skip(2)).Trim();

        if (!string.Equals(currentValue, oldValue, StringComparison.Ordinal))
            return null;

        // Reconstruct: key\tcontext\tnewValue
        return $"{parts[0]}\t{parts[1]}\t{newValue}";
    }
}
