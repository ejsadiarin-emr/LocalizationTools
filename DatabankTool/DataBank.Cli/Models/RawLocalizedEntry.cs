namespace DataBank.Cli.Models;

/// <summary>
/// Flat entry produced by parsers before grouping.
/// Parsers output one of these per key per locale.
/// </summary>
public class RawLocalizedEntry
{
    public required string Key { get; set; }
    public string? Context { get; set; }
    public required string Locale { get; set; }
    public required string Value { get; set; }
    public required SourceInfo Source { get; set; }
    public EntryMetadata Metadata { get; set; } = new();
}
