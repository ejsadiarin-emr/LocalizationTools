using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using MongoDB.Bson;

namespace DataBank.Import;

[Obsolete("Use POST /api/import endpoint instead. This tool will be removed in a future version.")]
public class Program
{
    public static async Task<int> Main(string[] args)
    {
        string inputPath = "data-bank.json";
        string? connectionString = null;

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--input" && i + 1 < args.Length)
                inputPath = args[++i];
            else if (args[i] == "--connection-string" && i + 1 < args.Length)
                connectionString = args[++i];
        }

        if (!File.Exists(inputPath))
        {
            Console.Error.WriteLine($"Error: File not found: {inputPath}");
            return 1;
        }

        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true)
            .Build();

        connectionString ??= configuration["MongoDb:ConnectionString"] ?? "mongodb://localhost:27017";
        string databaseName = configuration["MongoDb:DatabaseName"] ?? "databank";

        Console.WriteLine($"Reading {inputPath}...");

        var json = await File.ReadAllTextAsync(inputPath);
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var dataBank = JsonSerializer.Deserialize<DataBankInput>(json, options);

        if (dataBank?.Entries == null || dataBank.Entries.Count == 0)
        {
            Console.Error.WriteLine("Error: No entries found in input file.");
            return 1;
        }

        Console.WriteLine($"Found {dataBank.Entries.Count} entries (version {dataBank.Version}).");

        var client = new MongoClient(connectionString);
        var database = client.GetDatabase(databaseName);
        var entriesCollection = database.GetCollection<BsonDocument>("DataBankEntry");
        var metadataCollection = database.GetCollection<BsonDocument>("DataBankMetadata");

        var sw = Stopwatch.StartNew();
        const int batchSize = 1000;
        int total = dataBank.Entries.Count;
        int imported = 0;
        int errors = 0;

        for (int i = 0; i < total; i += batchSize)
        {
            var batch = dataBank.Entries.Skip(i).Take(batchSize).ToList();
            var documents = new List<BsonDocument>();

            foreach (var entry in batch)
            {
                try
                {
                    // Build Values array
                    var valuesArray = new BsonArray();
                    foreach (var val in entry.Values)
                    {
                        valuesArray.Add(new BsonDocument
                        {
                            { "Locale", val.Locale },
                            { "Value", val.Value }
                        });
                    }

                    // Build Sources dictionary
                    var sourcesDoc = new BsonDocument();
                    foreach (var kvp in entry.Sources)
                    {
                        sourcesDoc.Add(kvp.Key, new BsonDocument
                        {
                            { "Format", kvp.Value.Format },
                            { "File", kvp.Value.File },
                            { "Path", kvp.Value.Path }
                        });
                    }

                    var doc = new BsonDocument
                    {
                        { "_id", entry.Key },
                        { "Key", entry.Key },
                        { "Values", valuesArray },
                        { "Sources", sourcesDoc },
                        { "Metadata", new BsonDocument
                            {
                                { "Comment", entry.Metadata.Comment is null ? BsonNull.Value : entry.Metadata.Comment },
                                { "FormatSpecifiers", new BsonArray(entry.Metadata.FormatSpecifiers ?? []) },
                                { "DoNotTranslate", entry.Metadata.DoNotTranslate },
                                { "IsTranslated", entry.Metadata.IsTranslated }
                            }
                        }
                    };
                    documents.Add(doc);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"  Error processing entry {entry.Key}: {ex.Message}");
                    errors++;
                }
            }

            if (documents.Count > 0)
            {
                var models = documents.Select(doc => new ReplaceOneModel<BsonDocument>(
                    new BsonDocument("_id", doc["_id"]),
                    doc) { IsUpsert = true }).ToList();

                await entriesCollection.BulkWriteAsync(models);
            }

            imported += batch.Count;
            Console.WriteLine($"Importing entries: {imported}/{total}");
        }

        var metadataDoc = new BsonDocument
        {
            { "_id", "metadata" },
            { "Version", dataBank.Version },
            { "Generated", dataBank.Generated ?? DateTime.UtcNow.ToString("o") },
            { "EntryCount", total }
        };

        await metadataCollection.ReplaceOneAsync(
            new BsonDocument("_id", "metadata"),
            metadataDoc,
            new ReplaceOptions { IsUpsert = true });

        sw.Stop();
        Console.WriteLine($"Import complete: {total} entries in {sw.Elapsed.TotalSeconds:F1}s" + (errors > 0 ? $" ({errors} errors)" : ""));

        return 0;
    }
}

public class DataBankInput
{
    public int Version { get; set; }
    public string? Generated { get; set; }
    public List<LocalizedStringEntryInput> Entries { get; set; } = [];
}

public class LocalizedStringEntryInput
{
    public string Id { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public List<LocaleValueInput> Values { get; set; } = [];
    public Dictionary<string, SourceInfoInput> Sources { get; set; } = [];
    public EntryMetadataInput Metadata { get; set; } = new();
}

public class LocaleValueInput
{
    public string Locale { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

public class SourceInfoInput
{
    public string Format { get; set; } = string.Empty;
    public string File { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
}

public class EntryMetadataInput
{
    public string? Comment { get; set; }
    public List<string> FormatSpecifiers { get; set; } = [];
    public bool DoNotTranslate { get; set; }
    public bool IsTranslated { get; set; }
}
