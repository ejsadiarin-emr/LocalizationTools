using System.Text.Json;
using DataBank.Cli.Helpers;
using DataBank.Cli.Models;
using DataBank.Cli.Parsers;
using DataBank.Cli.Replacers;

namespace DataBank.Cli;

public class Program
{
    public static int Main(string[] args)
    {
        // Check for edit subcommand
        if (args.Length > 0 && args[0] == "edit")
        {
            return RunEditCommand(args.Skip(1).ToArray());
        }

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
        bool flagUntranslated = false;

        // Simple argument parsing
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--input-dir":
                    if (i + 1 < args.Length) inputDir = args[++i];
                    break;
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
                case "--flag-untranslated":
                    flagUntranslated = true;
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

        // Discover and parse files — parsers produce flat RawLocalizedEntry objects
        var rawEntries = new List<RawLocalizedEntry>();
        var rootDir = Path.GetFullPath(inputDir);

        var detectedFiles = FileDetector.DiscoverFiles(inputDir);

        foreach (var (file, detectedFormat) in detectedFiles)
        {
            if (format is not null && !format.Equals(detectedFormat, StringComparison.OrdinalIgnoreCase))
                continue;

            if (verbose) Console.Error.WriteLine($"Parsing: {Path.GetRelativePath(rootDir, file)}");

            switch (detectedFormat)
            {
                case "resx":
                    rawEntries.AddRange(ResxParser.Parse(file, rootDir));
                    break;
                case "rc":
                    rawEntries.AddRange(RcParser.Parse(file, symbolMap, rootDir));
                    break;
                case "fhx":
                    rawEntries.AddRange(FhxParser.Parse(file, localeOverride, encodingOverride, rootDir));
                    break;
                case "ahc":
                    rawEntries.AddRange(AhcParser.Parse(file, encodingOverride, rootDir));
                    break;
                case "json":
                    rawEntries.AddRange(JsonParser.Parse(file, rootDir));
                    break;
                case "grf":
                    rawEntries.AddRange(GrfParser.Parse(file, rootDir));
                    break;
            }
        }

        if (rawEntries.Count == 0)
        {
            Console.WriteLine("No localization entries found.");
            return 0;
        }

        // Group flat entries by key → one entry per key with multi-locale values
        var entries = EntryGrouper.GroupByKey(rawEntries);

        // Write output
        var outputPathValue = outputPath ?? Path.Combine(".", "data-bank.json");
        var outputDir = Path.GetDirectoryName(outputPathValue);
        if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
            Directory.CreateDirectory(outputDir);

        var output = new DataBankOutput
        {
            Generated = DateTime.UtcNow.ToString("o"),
            BasePath = rootDir,
            Entries = entries
        };

        if (flagUntranslated)
        {
            output.TranslationSummary = TranslationStatusAnalyzer.Analyze(entries);
        }

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
        };

        var json = JsonSerializer.Serialize(output, options);
        File.WriteAllText(outputPathValue, json);

        Console.WriteLine($"Wrote {entries.Count} entries ({rawEntries.Count} raw) to {outputPathValue}");

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
        Console.WriteLine($"Total keys: {entries.Count}");

        // Count total locale values
        var totalLocaleValues = entries.Sum(e => e.Values.Count);
        Console.WriteLine($"Total locale values: {totalLocaleValues}");

        Console.WriteLine();
        Console.WriteLine("By format (from sources):");
        var formatCounts = new Dictionary<string, int>();
        foreach (var entry in entries)
        {
            foreach (var source in entry.Sources.Values)
            {
                if (!formatCounts.ContainsKey(source.Format))
                    formatCounts[source.Format] = 0;
                formatCounts[source.Format]++;
            }
        }
        foreach (var (fmt, count) in formatCounts.OrderBy(kvp => kvp.Key))
        {
            Console.WriteLine($"  {fmt}: {count}");
        }

        Console.WriteLine();
        Console.WriteLine("By locale:");
        var localeCounts = new Dictionary<string, int>();
        foreach (var entry in entries)
        {
            foreach (var val in entry.Values)
            {
                if (!localeCounts.ContainsKey(val.Locale))
                    localeCounts[val.Locale] = 0;
                localeCounts[val.Locale]++;
            }
        }
        foreach (var (locale, count) in localeCounts.OrderBy(kvp => kvp.Key))
        {
            Console.WriteLine($"  {locale}: {count}");
        }
    }

    private static int RunEditCommand(string[] args)
    {
        string? key = null;
        string? locale = null;
        string? newValue = null;
        string? dataBankPath = null;
        bool dryRun = false;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--key" or "-k":
                    if (i + 1 < args.Length) key = args[++i];
                    break;
                case "--locale" or "-l":
                    if (i + 1 < args.Length) locale = args[++i];
                    break;
                case "--value" or "-v":
                    if (i + 1 < args.Length) newValue = args[++i];
                    break;
                case "--file" or "-f":
                    if (i + 1 < args.Length) dataBankPath = args[++i];
                    break;
                case "--dry-run":
                    dryRun = true;
                    break;
                case "--help" or "-h":
                    PrintEditUsage();
                    return 0;
            }
        }

        if (key is null || locale is null || newValue is null || dataBankPath is null)
        {
            Console.Error.WriteLine("Error: --key, --locale, --value, and --file are required for edit command.");
            PrintEditUsage();
            return 1;
        }

        if (!File.Exists(dataBankPath))
        {
            Console.Error.WriteLine($"Error: Data bank file not found: {dataBankPath}");
            return 1;
        }

        // Load data bank
        var json = File.ReadAllText(dataBankPath);
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
        };
        var dataBank = JsonSerializer.Deserialize<DataBankOutput>(json, options);

        if (dataBank?.Entries is null)
        {
            Console.Error.WriteLine("Error: Failed to load data bank or no entries found.");
            return 1;
        }

        // Find entry by key
        var entry = dataBank.Entries.FirstOrDefault(e =>
            string.Equals(e.Key, key, StringComparison.OrdinalIgnoreCase));

        if (entry is null)
        {
            Console.Error.WriteLine($"Error: Entry not found for key: {key}");
            return 1;
        }

        // Find locale value
        var localeValue = entry.Values.FirstOrDefault(v =>
            string.Equals(v.Locale, locale, StringComparison.OrdinalIgnoreCase));

        if (localeValue is null)
        {
            Console.Error.WriteLine($"Error: Locale '{locale}' not found for key: {key}");
            return 1;
        }

        // Find source for this locale
        if (!entry.Sources.TryGetValue(locale, out var sourceInfo))
        {
            Console.Error.WriteLine($"Error: No source information for locale '{locale}' on key: {key}");
            return 1;
        }

        // Resolve relative source path against the data bank base path
        var sourceFile = sourceInfo.File;
        if (!Path.IsPathRooted(sourceFile) && !string.IsNullOrEmpty(dataBank.BasePath))
        {
            sourceFile = Path.GetFullPath(Path.Combine(dataBank.BasePath, sourceFile));
        }

        // Show what will be changed
        Console.WriteLine($"Key:      {entry.Key}");
        Console.WriteLine($"Locale:   {locale}");
        Console.WriteLine($"File:     {sourceFile}");
        Console.WriteLine($"Line:     {sourceInfo.Line}");
        Console.WriteLine($"Format:   {sourceInfo.Format}");
        Console.WriteLine($"Old value: \"{localeValue.Value}\"");
        Console.WriteLine($"New value: \"{newValue}\"");

        if (dryRun)
        {
            Console.WriteLine();
            Console.WriteLine("(Dry run - no changes written)");
            return 0;
        }

        // Create raw entry for FileWriter
        var rawEntry = new RawLocalizedEntry
        {
            Key = entry.Key,
            Locale = locale,
            Value = localeValue.Value,
            Source = new SourceInfo
            {
                Format = sourceInfo.Format,
                File = sourceFile,
                Path = sourceFile,
                Line = sourceInfo.Line
            },
            Metadata = entry.Metadata
        };

        // Write back
        var writer = new FileWriter();
        var result = writer.EditEntry(rawEntry, newValue);

        if (result.Success)
        {
            Console.WriteLine();
            Console.WriteLine($"Successfully updated {result.File} at line {result.Line}");
            return 0;
        }
        else
        {
            Console.Error.WriteLine();
            Console.Error.WriteLine($"Error: {result.ErrorMessage}");
            return 1;
        }
    }

    private static void PrintEditUsage()
    {
        Console.WriteLine("databank-cli edit - Edit a translation value and write back to source file");
        Console.WriteLine();
        Console.WriteLine("Usage: databank-cli edit --key <key> --locale <locale> --value <value> --file <data-bank.json>");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --key, -k <key>        The entry key to edit (required)");
        Console.WriteLine("  --locale, -l <locale>  The locale to edit (required)");
        Console.WriteLine("  --value, -v <value>    The new translation value (required)");
        Console.WriteLine("  --file, -f <path>      Path to data-bank.json (required)");
        Console.WriteLine("  --dry-run              Show what would change without writing");
        Console.WriteLine("  --help, -h             Show this help message");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  databank-cli edit --key IDS_WELCOME --locale fr --value \"Bonjour\" --file data-bank.json");
        Console.WriteLine("  databank-cli edit -k IDS_START -l zh-CN -v \"启动\" -f data-bank.json --dry-run");
    }

    private static void PrintUsage()
    {
        Console.WriteLine("databank-cli - Localization string extractor");
        Console.WriteLine();
        Console.WriteLine("Usage: databank-cli [options]");
        Console.WriteLine("       databank-cli edit [options]");
        Console.WriteLine();
        Console.WriteLine("Commands:");
        Console.WriteLine("  (default)              Extract and group localization strings");
        Console.WriteLine("  edit                   Edit a translation value and write back to source");
        Console.WriteLine();
        Console.WriteLine("Extract Options:");
        Console.WriteLine("  --input-dir <path>     Input directory to scan (default: current directory)");
        Console.WriteLine("  --output, -o <path>    Output file path (default: ./data-bank.json)");
        Console.WriteLine("  --format, -f <format>  Filter by format: resx, rc, fhx, ahc, json");
        Console.WriteLine("  --resource-h <path>    Path to resource.h for .rc symbol resolution");
        Console.WriteLine("  --stats, -s            Print summary statistics");
        Console.WriteLine("  --coverage             Generate coverage analysis report");
        Console.WriteLine("  --coverage-output      Write coverage report to file (default: stdout)");
        Console.WriteLine("  --encoding <enc>       Override file encoding (e.g., windows-1252, cp936)");
        Console.WriteLine("  --locale <locale>      Override locale for FHX Translated files");
        Console.WriteLine("  --verbose, -v          Print per-file parsing progress to stderr");
        Console.WriteLine("  --flag-untranslated    Flag entries with translation status analysis");
        Console.WriteLine("  --help, -h             Show this help message");
        Console.WriteLine();
        Console.WriteLine("Edit Options:");
        Console.WriteLine("  --key, -k <key>        Entry key to edit");
        Console.WriteLine("  --locale, -l <locale>  Locale to edit");
        Console.WriteLine("  --value, -v <value>    New translation value");
        Console.WriteLine("  --file, -f <path>      Path to data-bank.json");
        Console.WriteLine("  --dry-run              Show what would change without writing");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  databank-cli --input-dir ./l10n-files");
        Console.WriteLine("  databank-cli --input-dir ./l10n-files --output ./out/data-bank.json --stats");
        Console.WriteLine("  databank-cli edit --key IDS_WELCOME --locale fr --value \"Bonjour\" --file data-bank.json");
    }
}
