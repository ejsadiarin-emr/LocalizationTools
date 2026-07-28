using System.Collections.Concurrent;
using System.Text.Json;
using DataBank.Cli.Models;
using DataBank.Cli.Parsers;

namespace DataBank.Api.Services;

public class ExtractionService : IExtractionService
{
    private readonly ConcurrentDictionary<string, ExtractionJob> _jobs = new();
    private readonly IDataBankService _dataBankService;
    private readonly ILogger<ExtractionService> _logger;

    public ExtractionService(IDataBankService dataBankService, ILogger<ExtractionService> logger)
    {
        _dataBankService = dataBankService;
        _logger = logger;
    }

    public string StartExtraction(string sourceDirectory, string[]? filePatterns = null)
    {
        var job = new ExtractionJob { SourceDirectory = sourceDirectory };
        _jobs[job.Id] = job;

        _ = Task.Run(() => RunExtraction(job, filePatterns));

        return job.Id;
    }

    public ExtractionJob? GetJobStatus(string jobId)
    {
        return _jobs.TryGetValue(jobId, out var job) ? job : null;
    }

    private void RunExtraction(ExtractionJob job, string[]? filePatterns)
    {
        try
        {
            if (!Directory.Exists(job.SourceDirectory))
            {
                job.Status = "failed";
                job.Errors.Add($"Source directory not found: {job.SourceDirectory}");
                job.CompletedAt = DateTime.UtcNow;
                return;
            }

            var patterns = filePatterns ?? ["*.resx", "*.rc", "*.fhx", "*.ahc"];
            var allFiles = new List<string>();
            foreach (var pattern in patterns)
            {
                allFiles.AddRange(Directory.GetFiles(job.SourceDirectory, pattern, SearchOption.AllDirectories));
            }

            var entries = new List<LocalizedStringEntry>();

            foreach (var file in allFiles)
            {
                try
                {
                    var ext = Path.GetExtension(file).ToLowerInvariant();
                    var parsedEntries = ext switch
                    {
                        ".resx" => ResxParser.Parse(file, job.SourceDirectory),
                        ".rc" => RcParser.Parse(file, rootDir: job.SourceDirectory),
                        ".fhx" => FhxParser.Parse(file, rootDir: job.SourceDirectory),
                        ".ahc" => AhcParser.Parse(file, rootDir: job.SourceDirectory),
                        _ => new List<LocalizedStringEntry>()
                    };
                    entries.AddRange(parsedEntries);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse file {File}", file);
                    job.Errors.Add($"Failed to parse {Path.GetFileName(file)}: {ex.Message}");
                }
            }

            job.EntriesExtracted = entries.Count;
            job.Status = "completed";
            job.CompletedAt = DateTime.UtcNow;

            _dataBankService.AddEntries(entries);

            _logger.LogInformation("Extraction job {JobId} completed: {Count} entries from {Files} files",
                job.Id, entries.Count, allFiles.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Extraction job {JobId} failed", job.Id);
            job.Status = "failed";
            job.Errors.Add(ex.Message);
            job.CompletedAt = DateTime.UtcNow;
        }
    }
}
