using System.Collections.Concurrent;
using System.Text.Json;
using DataBank.Api.Models;
using DataBank.Api.Repositories;
using DataBank.Cli.Helpers;
using DataBank.Cli.Models;
using DataBank.Cli.Parsers;
using Microsoft.AspNetCore.Mvc;

namespace DataBank.Api.Endpoints;

public static class ExtractionEndpoints
{
    private static readonly ConcurrentDictionary<string, ExtractionJob> Jobs = new();

    public static void MapExtractionEndpoints(this WebApplication app)
    {
        var extractGroup = app.MapGroup("/api/extract")
            .WithTags("Extraction");

        extractGroup.MapPost("/", async (
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

        extractGroup.MapGet("/{jobId}", (string jobId) =>
        {
            if (!Jobs.TryGetValue(jobId, out var job))
                return Results.NotFound(new { error = $"Job '{jobId}' not found." });

            return Results.Ok(job);
        })
        .WithName("GetExtractionJobStatus")
        .WithDescription("Get the status of an extraction job");

        var importGroup = app.MapGroup("/api/import")
            .WithTags("Import");

        importGroup.MapPost("/", async (
            IFormFile file,
            IDataBankRepository repository) =>
        {
            if (file == null || file.Length == 0)
                return Results.BadRequest(new { error = "No file uploaded." });

            if (!file.FileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                return Results.BadRequest(new { error = "File must be a JSON file." });

            try
            {
                using var stream = file.OpenReadStream();
                using var reader = new StreamReader(stream);
                var json = await reader.ReadToEndAsync();

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
                };
                var dataBank = JsonSerializer.Deserialize<DataBankOutput>(json, options);

                if (dataBank?.Entries == null)
                    return Results.BadRequest(new { error = "Invalid JSON structure." });

                var entries = dataBank.Entries.Select(e => new DataBankEntryDocument
                {
                    Id = e.Id,
                    Key = e.Key,
                    Context = e.Context,
                    Values = e.Values.Select(v => new LocaleValueDocument
                    {
                        Locale = v.Locale,
                        Value = v.Value
                    }).ToList(),
                    Sources = e.Sources.ToDictionary(
                        kvp => kvp.Key,
                        kvp => new SourceInfoDocument
                        {
                            Format = kvp.Value.Format,
                            File = kvp.Value.File,
                            Path = kvp.Value.Path,
                            Line = kvp.Value.Line
                        }),
                    Metadata = new EntryMetadataDocument
                    {
                        Comment = e.Metadata.Comment,
                        FormatSpecifiers = e.Metadata.FormatSpecifiers,
                        DoNotTranslate = e.Metadata.DoNotTranslate,
                        IsTranslated = e.Metadata.IsTranslated
                    }
                }).ToList();

                var importedCount = await repository.ReplaceOrInsertManyAsync(entries);

                var metadata = await repository.GetMetadataAsync() ?? new DataBankMetadataDocument { Id = "default" };
                metadata.Version = dataBank.Version;
                metadata.Generated = dataBank.Generated ?? DateTime.UtcNow.ToString("o");
                metadata.EntryCount = (int)await repository.GetEntryCountAsync();
                if (!string.IsNullOrEmpty(dataBank.BasePath))
                    metadata.BasePath = dataBank.BasePath;
                await repository.UpdateMetadataAsync(metadata);

                return Results.Ok(new
                {
                    success = true,
                    entryCount = importedCount,
                    version = dataBank.Version
                });
            }
            catch (JsonException ex)
            {
                return Results.BadRequest(new { error = $"Invalid JSON format: {ex.Message}" });
            }
            catch (Exception ex)
            {
                return Results.Problem(new { error = $"Import failed: {ex.Message}" }.ToString());
            }
        })
        .DisableAntiforgery()
        .WithName("ImportDataBankJson")
        .WithDescription("Import a data-bank.json file into MongoDB");

        var healthGroup = app.MapGroup("/api/health")
            .WithTags("Health");

        healthGroup.MapGet("/", async (IDataBankRepository repository) =>
        {
            try
            {
                var entryCount = await repository.GetEntryCountAsync();
                var metadata = await repository.GetMetadataAsync();

                return Results.Ok(new
                {
                    status = "healthy",
                    entryCount,
                    version = metadata?.Version ?? 0,
                    basePath = metadata?.BasePath
                });
            }
            catch (Exception ex)
            {
                return Results.Json(new
                {
                    status = "unhealthy",
                    error = ex.Message
                }, statusCode: 503);
            }
        })
        .WithName("HealthCheck")
        .WithDescription("Check API health and MongoDB connectivity");
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

            // Parsers now produce flat RawLocalizedEntry objects
            var rawEntries = new List<RawLocalizedEntry>();

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
                        _ => new List<RawLocalizedEntry>()
                    };

                    rawEntries.AddRange(parsedEntries);
                }
                catch (Exception ex)
                {
                    job.Errors.Add($"Failed to parse {Path.GetFileName(file)}: {ex.Message}");
                }
            }

            // Group flat entries by key
            var groupedEntries = EntryGrouper.GroupByKey(rawEntries);

            // Convert to DataBankEntryDocuments for MongoDB
            var documents = groupedEntries.Select(e => new DataBankEntryDocument
            {
                Id = e.Id,
                Key = e.Key,
                Context = e.Context,
                Values = e.Values.Select(v => new LocaleValueDocument
                {
                    Locale = v.Locale,
                    Value = v.Value
                }).ToList(),
                Sources = e.Sources.ToDictionary(
                    kvp => kvp.Key,
                    kvp => new SourceInfoDocument
                    {
                        Format = kvp.Value.Format,
                        File = kvp.Value.File,
                        Path = kvp.Value.Path,
                        Line = kvp.Value.Line
                    }),
                Metadata = new EntryMetadataDocument
                {
                    Comment = e.Metadata.Comment,
                    FormatSpecifiers = e.Metadata.FormatSpecifiers,
                    DoNotTranslate = e.Metadata.DoNotTranslate,
                    IsTranslated = e.Metadata.IsTranslated
                }
            }).ToList();

            await repository.InsertManyEntriesAsync(documents);

            var metadata = await repository.GetMetadataAsync() ?? new DataBankMetadataDocument { Id = "default" };
            metadata.Version = 3;
            metadata.Generated = DateTime.UtcNow.ToString("o");
            metadata.EntryCount = (int)await repository.GetEntryCountAsync();
            metadata.BasePath = job.SourceDirectory;
            await repository.UpdateMetadataAsync(metadata);

            job.EntriesExtracted = documents.Count;
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
