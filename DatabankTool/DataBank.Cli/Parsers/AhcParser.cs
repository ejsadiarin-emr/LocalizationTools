using System.Xml.Linq;
using DataBank.Cli.Helpers;
using DataBank.Cli.Models;

namespace DataBank.Cli.Parsers;

public static class AhcParser
{
    public static List<LocalizedStringEntry> Parse(string filePath, string? encodingOverride = null, string? rootDir = null)
    {
        var entries = new List<LocalizedStringEntry>();

        try
        {
            var content = EncodingDetector.ReadFile(filePath, encodingOverride);
            var relativePath = rootDir is not null
                ? Path.GetRelativePath(rootDir, filePath)
                : Path.GetFileName(filePath);

            var doc = XDocument.Parse(content);
            if (doc.Root is null)
                return entries;

            foreach (var languageValue in doc.Descendants("LanguageValue"))
            {
                var language = languageValue.Attribute("Name")?.Value;
                if (string.IsNullOrEmpty(language))
                    continue;

                var contentElement = languageValue.Element("Content");
                var text = contentElement?.Value?.Trim() ?? string.Empty;

                if (string.IsNullOrEmpty(text))
                    continue;

                entries.Add(new LocalizedStringEntry
                {
                    Id = $"ahc::{relativePath}::{language}::{GenerateKey(languageValue)}",
                    Key = GenerateKey(languageValue),
                    Value = text,
                    Locale = language,
                    Source = new SourceInfo
                    {
                        Format = "ahc",
                        File = relativePath,
                        Path = relativePath
                    },
                    Metadata = new EntryMetadata()
                });
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Warning: Failed to parse AHC file {filePath}: {ex.Message}");
        }

        return entries;
    }

    private static string GenerateKey(XElement languageValue)
    {
        // Walk up to find a meaningful parent name for the key
        var parent = languageValue.Parent;
        while (parent is not null)
        {
            if (parent.Name.LocalName == "LanguageValues")
            {
                parent = parent.Parent;
                continue;
            }

            // Use the first meaningful ancestor with a Name attribute
            var nameAttr = parent.Attribute("Name")?.Value;
            if (nameAttr is not null)
                return nameAttr;

            return parent.Name.LocalName;
        }

        return "unknown";
    }
}
