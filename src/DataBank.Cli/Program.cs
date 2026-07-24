using System.Text.Json;
using DataBank.Cli.Helpers;
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
        string? encodingOverride = null;
        string? localeOverride = null;
        bool showStats = false;
        bool showCoverage = false;
        string? coverageOutputPath = null;
        bool verbose = false;

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
                case "--encoding":
                    if (i + 1 < args.Length) encodingOverride = args[++i];
                    break;
                case "--locale":
                    if (i + 1 < args.Length) localeOverride = args[++i];
                    break;
                case "--stats" or "-s":
                    showStats = true;
                    break;
                case "--coverage":
                    showCoverage = true;
                    break;
                case "--coverage-output":
                    if (i + 1 < args.Length) coverageOutputPath = args[++i];
                    break;
                case "--verbose" or "-v":
                    verbose = true;
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
        var rootDir = Path.GetFullPath(inputDir);

        if (format is null || format.Equals("resx", StringComparison.OrdinalIgnoreCase))
        {
            var resxFiles = Directory.GetFiles(inputDir, "*.resx", SearchOption.AllDirectories);
            foreach (var file in resxFiles)
            {
                if (verbose) Console.Error.WriteLine($"Parsing: {Path.GetRelativePath(rootDir, file)}");
                entries.AddRange(ResxParser.Parse(file, rootDir));
            }
        }

        if (format is null || format.Equals("rc", StringComparison.OrdinalIgnoreCase))
        {
            var rcFiles = Directory.GetFiles(inputDir, "*.rc", SearchOption.AllDirectories);
            foreach (var file in rcFiles)
            {
                if (verbose) Console.Error.WriteLine($"Parsing: {Path.GetRelativePath(rootDir, file)}");
                entries.AddRange(RcParser.Parse(file, symbolMap, rootDir));
            }
        }

        if (format is null || format.Equals("fhx", StringComparison.OrdinalIgnoreCase))
        {
            var fhxFiles = Directory.GetFiles(inputDir, "*.txt", SearchOption.AllDirectories)
                .Where(f => Path.GetFileName(f).Equals("AlarmWords.txt", StringComparison.OrdinalIgnoreCase))
                .ToList();
            foreach (var file in fhxFiles)
            {
                if (verbose) Console.Error.WriteLine($"Parsing: {Path.GetRelativePath(rootDir, file)}");
                entries.AddRange(FhxParser.Parse(file, localeOverride, encodingOverride, rootDir));
            }
        }

        if (format is null || format.Equals("ahc", StringComparison.OrdinalIgnoreCase))
        {
            var ahcFiles = Directory.GetFiles(inputDir, "*.ahc", SearchOption.AllDirectories);
            foreach (var file in ahcFiles)
            {
                if (verbose) Console.Error.WriteLine($"Parsing: {Path.GetRelativePath(rootDir, file)}");
                entries.AddRange(AhcParser.Parse(file, encodingOverride, rootDir));
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

        if (showCoverage)
        {
            var coverageReport = CoverageAnalyzer.Analyze(entries, rootDir);

            if (coverageOutputPath is not null)
            {
                var coverageDir = Path.GetDirectoryName(coverageOutputPath);
                if (!string.IsNullOrEmpty(coverageDir) && !Directory.Exists(coverageDir))
                    Directory.CreateDirectory(coverageDir);

                var coverageJson = JsonSerializer.Serialize(coverageReport, options);
                File.WriteAllText(coverageOutputPath, coverageJson);
                Console.WriteLine($"Coverage report written to {coverageOutputPath}");
            }
            else
            {
                Console.WriteLine();
                Console.WriteLine("=== Coverage Report ===");
                Console.WriteLine($"Overall completion: {coverageReport.Summary.OverallCompletionPercentage}%");
                Console.WriteLine($"Total EN keys: {coverageReport.Summary.TotalEnKeys}");
                Console.WriteLine($"Total translated keys: {coverageReport.Summary.TotalTranslatedKeys}");
                Console.WriteLine($"Missing keys: {coverageReport.Summary.TotalMissingKeys}");
                Console.WriteLine($"Orphaned keys: {coverageReport.Summary.TotalOrphanedKeys}");

                if (coverageReport.Summary.ByLocale.Count > 0)
                {
                    Console.WriteLine();
                    Console.WriteLine("By locale:");
                    foreach (var locale in coverageReport.Summary.ByLocale)
                    {
                        Console.WriteLine($"  {locale.Locale}: {locale.CompletionPercentage}% ({locale.TranslatedKeys}/{locale.EnKeys})");
                    }
                }
            }
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
        Console.WriteLine("  --format, -f <format>  Filter by format: resx, rc, fhx, ahc");
        Console.WriteLine("  --resource-h <path>    Path to resource.h for .rc symbol resolution");
        Console.WriteLine("  --stats, -s            Print summary statistics");
        Console.WriteLine("  --coverage             Generate coverage analysis report");
        Console.WriteLine("  --coverage-output      Write coverage report to file (default: stdout)");
        Console.WriteLine("  --encoding <enc>       Override file encoding (e.g., windows-1252, cp936)");
        Console.WriteLine("  --locale <locale>      Override locale for FHX Translated files");
        Console.WriteLine("  --verbose, -v          Print per-file parsing progress to stderr");
        Console.WriteLine("  --help, -h             Show this help message");
    }
}
