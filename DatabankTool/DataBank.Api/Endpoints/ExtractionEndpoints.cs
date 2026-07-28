using System.Collections.Concurrent;
using DataBank.Api.Models;
using DataBank.Api.Repositories;
using DataBank.Cli.Models;
using DataBank.Cli.Parsers;
using Microsoft.AspNetCore.Mvc;

namespace DataBank.Api.Endpoints;

public static class ExtractionEndpoints
{
    private static readonly ConcurrentDictionary<string, ExtractionJob> Jobs = new();

    public static void MapExtractionEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/extract")
            .WithTags("Extraction");

        group.MapPost("/", async (
            ExtractRequest request,
            IDataBankRepository repository) =>
        {
            if (string.IsNullOrEmpty(request.SourceDirectory))
                return Results.BadRequest(new { error = "SourceDirectory is required." });

            if (!Directory.Exists(request.SourceDirectory))
                return Results.BadRequest(new { error = $"Source directory not found: {request.SourceDirectory}" });

            var job = new ExtractionJob { SourceDirectory = request.SourceDirectory };
            Jobs[job.Id] = job;

            _ = Task.Run(() => RunExtraction(job, request.FilePatterns, repository));

            return Results.Accepted($"/api/extract/{job.Id}", new { jobId = job.Id, message = "Extraction job started." });
        })
        .WithName("StartExtraction")
        .WithDescription("Trigger file parsing and data extraction into MongoDB");

        group.MapGet("/{jobId}", (string jobId) =>
        {
            if (!Jobs.TryGetValue(jobId, out var job))
                return Results.NotFound(new { error = $"Job '{jobId}' not found." });

            return Results.Ok(job);
        })
        .WithName("GetExtractionJobStatus")
        .WithDescription("Get the status of an extraction job");
    }

    private static async Task RunExtraction(ExtractionJob job, string[]? filePatterns, IDataBankRepository repository)
    {
        try
        {
            var patterns = filePatterns ?? ["*.resx", "*.rc", "*.fhx", "*.ahc"];
            var allFiles = new List<string>();
            foreach (var pattern in patterns)
            {
                allFiles.AddRange(Directory.GetFiles(job.SourceDirectory, pattern, SearchOption.AllDirectories));
            }

            var entries = new List<DataBankEntryDocument>();

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

                    foreach (var entry in parsedEntries)
                    {
                        entries.Add(new DataBankEntryDocument
                        {
                            Id = entry.Id,
                            Key = entry.Key,
                            Value = entry.Value,
                            Locale = entry.Locale,
                            Source = new SourceInfoDocument
                            {
                                Format = entry.Source.Format,
                                File = entry.Source.File,
                                Path = entry.Source.Path,
                                Encoding = entry.Source.Encoding
                            },
                            Metadata = new EntryMetadataDocument
                            {
                                Comment = entry.Metadata.Comment,
                                RcId = entry.Metadata.RcId,
                                RcDefine = entry.Metadata.RcDefine,
                                IsBehavioral = entry.Metadata.IsBehavioral,
                                FormatSpecifiers = entry.Metadata.FormatSpecifiers,
                                DoNotTranslate = entry.Metadata.DoNotTranslate,
                                IsTranslated = entry.Metadata.IsTranslated,
                                TranslationStatus = entry.Metadata.TranslationStatus.ToString()
                            }
                        });
                    }
                }
                catch (Exception ex)
                {
                    job.Errors.Add($"Failed to parse {Path.GetFileName(file)}: {ex.Message}");
                }
            }

            await repository.InsertManyEntriesAsync(entries);

            var metadata = await repository.GetMetadataAsync() ?? new DataBankMetadataDocument { Id = "default" };
            metadata.Version = 2;
            metadata.Generated = DateTime.UtcNow.ToString("o");
            metadata.EntryCount = (int)await repository.GetEntryCountAsync();
            await repository.UpdateMetadataAsync(metadata);

            job.EntriesExtracted = entries.Count;
            job.Status = "completed";
            job.CompletedAt = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            job.Status = "failed";
            job.Errors.Add(ex.Message);
            job.CompletedAt = DateTime.UtcNow;
        }
    }
}

public class ExtractionJob
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string SourceDirectory { get; set; } = string.Empty;
    public string Status { get; set; } = "running";
    public int EntriesExtracted { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public List<string> Errors { get; set; } = [];
}
