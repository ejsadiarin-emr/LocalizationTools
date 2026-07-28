using System.Text.Json;
using DataBank.Cli.Helpers;
using DataBank.Cli.Models;

namespace DataBank.Cli.Parsers;

public static class JsonParser
{
    public static List<LocalizedStringEntry> Parse(string filePath, string? rootDir = null)
    {
        var entries = new List<LocalizedStringEntry>();
        var relativePath = rootDir is not null
            ? Path.GetRelativePath(rootDir, filePath)
            : Path.GetFileName(filePath);

        var locale = DetectLocale(filePath);
        var isDntFile = FileHelper.HasDntInFilename(filePath);

        try
        {
            var content = File.ReadAllText(filePath);
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

                entries.Add(new LocalizedStringEntry
                {
                    Id = $"json::{relativePath}::{property.Name}",
                    Key = property.Name,
                    Value = value,
                    Locale = locale,
                    Source = new SourceInfo
                    {
                        Format = "json",
                        File = relativePath,
                        Path = relativePath
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

    internal static string DetectLocale(string filePath)
    {
        var fileName = Path.GetFileNameWithoutExtension(filePath);

        if (!fileName.StartsWith("translate.", StringComparison.OrdinalIgnoreCase))
            return "en";

        var locale = fileName["translate.".Length..];
        return string.IsNullOrEmpty(locale) ? "en" : locale;
    }
}
