using DataBank.Cli.Helpers;
using DataBank.Cli.Models;

namespace DataBank.Cli.Parsers;

public static class FhxParser
{
    public static List<LocalizedStringEntry> Parse(string filePath, string? localeOverride = null, string? encodingOverride = null, string? rootDir = null)
    {
        var entries = new List<LocalizedStringEntry>();

        try
        {
            var content = EncodingDetector.ReadFile(filePath, encodingOverride);
            var locale = DetectLocale(filePath, localeOverride);
            var relativePath = rootDir is not null
                ? Path.GetRelativePath(rootDir, filePath)
                : Path.GetFileName(filePath);

            foreach (var rawLine in content.Split(["\r\n", "\n", "\r"], StringSplitOptions.None))
            {
                var line = rawLine.TrimEnd('\r', '\n');
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var entry = ParseLine(line, locale, relativePath);
                if (entry is not null)
                    entries.Add(entry);
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Warning: Failed to parse FHX file {filePath}: {ex.Message}");
        }

        return entries;
    }

    private static LocalizedStringEntry? ParseLine(string line, string locale, string relativePath)
    {
        // Format: @Key@\t"context"\tValue
        var parts = line.Split('\t');
        if (parts.Length < 2)
            return null;

        var key = parts[0].Trim();
        if (string.IsNullOrEmpty(key))
            return null;

        // Context is the second field (may be quoted)
        var context = parts.Length >= 2 ? parts[1].Trim().Trim('"') : string.Empty;

        // Value is everything after the second tab (may contain tabs)
        var value = parts.Length >= 3 ? string.Join("\t", parts.Skip(2)).Trim() : string.Empty;

        var doNotTranslate = context.Contains("do NOT translate", StringComparison.OrdinalIgnoreCase);

        var metadata = new EntryMetadata
        {
            DoNotTranslate = doNotTranslate
        };

        RcParser.DetectFormatSpecifiers(value, metadata);

        return new LocalizedStringEntry
        {
            Id = $"fhx::{relativePath}::{key}",
            Key = key,
            Value = value,
            Locale = locale,
            Source = new SourceInfo
            {
                Format = "fhx",
                File = relativePath,
                Path = relativePath
            },
            Metadata = metadata
        };
    }

    internal static string DetectLocale(string filePath, string? localeOverride = null)
    {
        if (localeOverride is not null)
            return localeOverride;

        // Look at parent directory name
        var parentDir = Path.GetFileName(Path.GetDirectoryName(filePath));

        if (string.IsNullOrEmpty(parentDir))
            return "en";

        return parentDir.ToUpperInvariant() switch
        {
            "EN" => "en",
            _ => parentDir.ToLowerInvariant()
        };
    }
}
