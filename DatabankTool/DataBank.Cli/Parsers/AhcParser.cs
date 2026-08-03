using System.Xml;
using System.Xml.Linq;
using DataBank.Cli.Helpers;
using DataBank.Cli.Models;

namespace DataBank.Cli.Parsers;

public static class AhcParser
{
    public static List<RawLocalizedEntry> Parse(string filePath, string? encodingOverride = null, string? rootDir = null)
    {
        var entries = new List<RawLocalizedEntry>();

        try
        {
            var content = EncodingDetector.ReadFile(filePath, encodingOverride);
            var relativePath = rootDir is not null
                ? Path.GetRelativePath(rootDir, filePath)
                : Path.GetFileName(filePath);

            var isDntFile = FileHelper.HasDntInFilename(filePath);

            var doc = XDocument.Parse(content, LoadOptions.SetLineInfo);
            if (doc.Root is null)
                return entries;

            // Collect all locales defined in the file for empty-value entries
            var allLocales = doc.Descendants("LanguageValue")
                .Select(lv => lv.Attribute("Name")?.Value)
                .Where(l => !string.IsNullOrEmpty(l))
                .Distinct()
                .ToList();

            foreach (var languageValue in doc.Descendants("LanguageValue"))
            {
                var language = languageValue.Attribute("Name")?.Value;
                if (string.IsNullOrEmpty(language))
                    continue;

                var key = GenerateKey(languageValue);
                if (key is null)
                    continue;

                var contentElement = languageValue.Element("Content");
                var text = contentElement?.Value?.Trim() ?? string.Empty;

                if (string.IsNullOrEmpty(text))
                    continue;

                entries.Add(new RawLocalizedEntry
                {
                    Key = key,
                    Value = text,
                    Locale = language,
                    Source = new SourceInfo
                    {
                        Format = "ahc",
                        File = relativePath,
                        Path = relativePath,
                        Line = languageValue is IXmlLineInfo lineInfo && lineInfo.HasLineInfo() ? lineInfo.LineNumber : null
                    },
                    Metadata = new EntryMetadata { DoNotTranslate = isDntFile }
                });
            }

            // Scan for Text/CheckBox elements under ContainedElements with no LanguageValue descendants
            var containedElements = doc.Root.Element("ContainedElements");
            if (containedElements is not null)
            {
                foreach (var element in containedElements.Elements()
                    .Where(e => e.Name.LocalName is "Text" or "CheckBox"))
                {
                    var nameAttr = element.Attribute("Name")?.Value;
                    if (string.IsNullOrEmpty(nameAttr))
                        continue;

                    // Skip if this element has LanguageValues with actual content
                    var hasNonEmptyLanguageValues = element.Descendants("LanguageValue")
                        .Any(lv => !string.IsNullOrWhiteSpace(lv.Element("Content")?.Value));
                    if (hasNonEmptyLanguageValues)
                        continue;

                    foreach (var locale in allLocales)
                    {
                        entries.Add(new RawLocalizedEntry
                        {
                            Key = nameAttr,
                            Value = string.Empty,
                            Locale = locale,
                            Source = new SourceInfo
                            {
                                Format = "ahc",
                                File = relativePath,
                                Path = relativePath,
                                Line = element is IXmlLineInfo lineInfo && lineInfo.HasLineInfo() ? lineInfo.LineNumber : null
                            },
                            Metadata = new EntryMetadata
                            {
                                DoNotTranslate = isDntFile,
                                IsTranslated = false,
                                Comment = "no language value provided"
                            }
                        });
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Warning: Failed to parse AHC file {filePath}: {ex.Message}");
        }

        return entries;
    }

    private static string? GenerateKey(XElement languageValue)
    {
        var ancestors = languageValue.Ancestors().ToList();

        // Skip if any ancestor is a Gem (internal graphic variable, not a translatable key)
        if (ancestors.Any(a => a.Name.LocalName == "Gem"))
            return null;

        // Walk up, skipping LanguageValues and root element
        foreach (var ancestor in ancestors)
        {
            if (ancestor.Name.LocalName == "LanguageValues")
                continue;

            // Skip the root element (ContextualDisplay) - it has a Name but is not a key
            if (ancestor.Parent is null)
                continue;

            var nameAttr = ancestor.Attribute("Name")?.Value;
            if (nameAttr is not null)
                return nameAttr;
        }

        // No Name attribute found on a non-root element.
        // For top-level Title/Description (direct children of root), use the element name.
        var firstNonLanguageValues = ancestors.FirstOrDefault(a => a.Name.LocalName != "LanguageValues");
        if (firstNonLanguageValues?.Name.LocalName is "Title" or "Description")
            return firstNonLanguageValues.Name.LocalName;

        // Tag, Context1Description, GsLocalizedString, etc. at root level - skip
        return null;
    }
}
