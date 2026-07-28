namespace DataBank.Api.Models;

public class CreateEntryRequest
{
    public string? Id { get; set; }
    public string? Key { get; set; }
    public string? Value { get; set; }
    public string? Locale { get; set; }
    public DataBank.Cli.Models.SourceInfo? Source { get; set; }
    public DataBank.Cli.Models.EntryMetadata? Metadata { get; set; }
}

public class PaginatedResult<T>
{
    public List<T> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public class ExtractRequest
{
    public string SourceDirectory { get; set; } = string.Empty;
    public string[]? FilePatterns { get; set; }
}
