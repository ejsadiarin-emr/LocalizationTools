using DataBank.Cli.Models;

namespace DataBank.Api.Services;

public interface IExtractionService
{
    string StartExtraction(string sourceDirectory, string[]? filePatterns = null);
    ExtractionJob? GetJobStatus(string jobId);
}

public class ExtractionJob
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Status { get; set; } = "running";
    public string SourceDirectory { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public int EntriesExtracted { get; set; }
    public List<string> Errors { get; set; } = [];
}
