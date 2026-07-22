using System.Text.Json;
using DataBank.Cli.Models;
using DataBank.Cli.Parsers;

namespace DataBank.Cli;

public class Program
{
    public static int Main(string[] args)
    {
        var inputDir = ".";
        string? outputPath = null;
        string? format = null;
        string? resourceH = null;
        bool showStats = false;

        // Simple argument parsing
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--output" or "-o":
                    if (i + 1 < args.Length) outputPath = args[++i];
                    break;
                case "--format" or "-f":
                    if (i + 1 < args.Length) format = args[++i];
                    break;
                case "--resource-h":
                    if (i + 1 < args.Length) resourceH = args[++i];
                    break;
                case "--stats" or "-s":
                    showStats = true;
                    break;
                case "--help" or "-h":
                    PrintUsage();
                    return 0;
                default:
                    inputDir = args[i];
                    break;
            }
        }

        if (!Directory.Exists(inputDir))
        {
            Console.Error.WriteLine($"Error: Directory not found: {inputDir}");
            return 1;
        }

        // Resolve resource.h if provided
        Dictionary<int, string>? symbolMap = null;
        if (resourceH is not null)
        {
            if (!File.Exists(resourceH))
            {
                Console.Error.WriteLine($"Error: resource.h not found: {resourceH}");
                return 1;
            }
            symbolMap = RcParser.ParseResourceH(resourceH);
        }

        // Discover and parse files
        var entries = new List<LocalizedStringEntry>();

        if (format is null || format.Equals("resx", StringComparison.OrdinalIgnoreCase))
        {
            var resxFiles = Directory.GetFiles(inputDir, "*.resx", SearchOption.AllDirectories);
            foreach (var file in resxFiles)
            {
                entries.AddRange(ResxParser.Parse(file));
            }
        }

        if (format is null || format.Equals("rc", StringComparison.OrdinalIgnoreCase))
        {
            var rcFiles = Directory.GetFiles(inputDir, "*.rc", SearchOption.AllDirectories);
            foreach (var file in rcFiles)
            {
                entries.AddRange(RcParser.Parse(file, symbolMap));
            }
        }

        if (entries.Count == 0)
        {
            Console.WriteLine("No localization entries found.");
            return 0;
        }

        // Write output
        var outputPathValue = outputPath ?? Path.Combine(".", "data-bank.json");
        var outputDir = Path.GetDirectoryName(outputPathValue);
        if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
            Directory.CreateDirectory(outputDir);

        var output = new DataBankOutput
        {
            Generated = DateTime.UtcNow.ToString("o"),
            Entries = entries
        };

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var json = JsonSerializer.Serialize(output, options);
        File.WriteAllText(outputPathValue, json);

        Console.WriteLine($"Wrote {entries.Count} entries to {outputPathValue}");

        if (showStats)
        {
            PrintStats(entries);
        }

        return 0;
    }

    private static void PrintStats(List<LocalizedStringEntry> entries)
    {
        Console.WriteLine();
        Console.WriteLine("=== Statistics ===");
        Console.WriteLine($"Total entries: {entries.Count}");

        Console.WriteLine();
        Console.WriteLine("By format:");
        foreach (var group in entries.GroupBy(e => e.Source.Format).OrderBy(g => g.Key))
        {
            Console.WriteLine($"  {group.Key}: {group.Count()}");
        }

        Console.WriteLine();
        Console.WriteLine("By locale:");
        foreach (var group in entries.GroupBy(e => e.Locale).OrderBy(g => g.Key))
        {
            Console.WriteLine($"  {group.Key}: {group.Count()}");
        }

        Console.WriteLine();
        Console.WriteLine("By file:");
        foreach (var group in entries.GroupBy(e => e.Source.File).OrderBy(g => g.Key))
        {
            Console.WriteLine($"  {group.Key}: {group.Count()}");
        }
    }

    private static void PrintUsage()
    {
        Console.WriteLine("dv-extract - Localization string extractor");
        Console.WriteLine();
        Console.WriteLine("Usage: dv-extract [input-directory] [options]");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --output, -o <path>    Output file path (default: ./data-bank.json)");
        Console.WriteLine("  --format, -f <format>  Filter by format: resx or rc");
        Console.WriteLine("  --resource-h <path>    Path to resource.h for .rc symbol resolution");
        Console.WriteLine("  --stats, -s            Print summary statistics");
        Console.WriteLine("  --help, -h             Show this help message");
    }
}
