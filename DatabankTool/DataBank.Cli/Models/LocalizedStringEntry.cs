namespace DataBank.Cli.Models;

public class LocalizedStringEntry
{
    public required string Id { get; set; }
    public required string Key { get; set; }
    public required string Value { get; set; }
    public required string Locale { get; set; }
    public required SourceInfo Source { get; set; }
    public EntryMetadata Metadata { get; set; } = new();
}
