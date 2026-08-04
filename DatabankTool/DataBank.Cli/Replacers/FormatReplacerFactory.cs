namespace DataBank.Cli.Replacers;

public static class FormatReplacerFactory
{
    private static readonly Dictionary<string, IFormatReplacer> Replacers = new(StringComparer.OrdinalIgnoreCase)
    {
        ["rc"] = new RcReplacer(),
        ["fhx"] = new FhxReplacer(),
        ["resx"] = new ResxReplacer(),
        ["ahc"] = new AhcReplacer(),
        ["json"] = new JsonReplacer()
    };

    public static IFormatReplacer? GetReplacer(string format)
    {
        return Replacers.TryGetValue(format, out var replacer) ? replacer : null;
    }
}
