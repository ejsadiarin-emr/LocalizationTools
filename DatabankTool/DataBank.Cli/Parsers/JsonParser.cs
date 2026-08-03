using System.Text.Json;
using DataBank.Cli.Helpers;
using DataBank.Cli.Models;

namespace DataBank.Cli.Parsers;

public static class JsonParser
{
    public static List<RawLocalizedEntry> Parse(string filePath, string? rootDir = null)
    {
        var entries = new List<RawLocalizedEntry>();
        var relativePath = rootDir is not null
            ? Path.GetRelativePath(rootDir, filePath)
            : Path.GetFileName(filePath);

        var locale = DetectLocale(filePath);
        var isDntFile = FileHelper.HasDntInFilename(filePath);

        try
        {
            var content = File.ReadAllText(filePath);

            // Build a line index: map each character position to its line number
            var lineMap = BuildLineMap(content);

            using var doc = JsonDocument.Parse(content);

            if (doc.RootElement.ValueKind != JsonValueKind.Object)
            {
                Console.Error.WriteLine($"Warning: Expected flat JSON object in {filePath}, got {doc.RootElement.ValueKind}");
                return entries;
            }

            foreach (var property in doc.RootElement.EnumerateObject())
            {
                if (property.Value.ValueKind != JsonValueKind.String)
                    continue;

                var value = property.Value.GetString() ?? string.Empty;
                if (string.IsNullOrEmpty(value))
                    continue;

                var line = FindKeyLine(content, property.Name, lineMap);

                entries.Add(new RawLocalizedEntry
                {
                    Key = property.Name,
                    Value = value,
                    Locale = locale,
                    Source = new SourceInfo
                    {
                        Format = "json",
                        File = relativePath,
                        Path = relativePath,
                        Line = line
                    },
                    Metadata = new EntryMetadata { DoNotTranslate = isDntFile }
                });
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Warning: Failed to parse JSON file {filePath}: {ex.Message}");
        }

        return entries;
    }

    private static List<int> BuildLineMap(string content)
    {
        var lineMap = new List<int>();
        var lineNum = 1;
        for (var i = 0; i < content.Length; i++)
        {
            lineMap.Add(lineNum);
            if (content[i] == '\n')
                lineNum++;
        }
        return lineMap;
    }

    private static int? FindKeyLine(string content, string key, List<int> lineMap)
    {
        // Search for the key pattern in the raw content: "key":
        var searchPattern = $"\"{key}\"";
        var index = content.IndexOf(searchPattern, StringComparison.Ordinal);
        if (index < 0)
            return null;

        return lineMap[index];
    }

    internal static string DetectLocale(string filePath)
    {
        var fileName = Path.GetFileNameWithoutExtension(filePath);

        if (!fileName.StartsWith("translate.", StringComparison.OrdinalIgnoreCase))
            return "en";

        var locale = fileName["translate.".Length..];
        return string.IsNullOrEmpty(locale) ? "en" : locale;
    }
}
