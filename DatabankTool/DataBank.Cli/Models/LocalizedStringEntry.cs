namespace DataBank.Cli.Models;

public class LocaleValue
{
    public required string Locale { get; set; }
    public required string Value { get; set; }
}

public class LocalizedStringEntry
{
    public required string Id { get; set; }
    public required string Key { get; set; }
    public List<LocaleValue> Values { get; set; } = [];
    public Dictionary<string, SourceInfo> Sources { get; set; } = [];
    public EntryMetadata Metadata { get; set; } = new();
}
