using System.Xml;
using System.Xml.Linq;
using DataBank.Cli.Helpers;
using DataBank.Cli.Models;

namespace DataBank.Cli.Parsers;

public static partial class ResxParser
{
    private static readonly XNamespace Xsd = "http://www.w3.org/2001/XMLSchema";
    private static readonly XNamespace MsData = "urn:schemas-microsoft-com:xml-msdata";

    public static List<RawLocalizedEntry> Parse(string filePath, string? rootDir = null)
    {
        var entries = new List<RawLocalizedEntry>();
        var locale = DetectLocale(filePath);
        var relativePath = rootDir is not null
            ? Path.GetRelativePath(rootDir, filePath)
            : Path.GetFileName(filePath);

        var isDntFile = FileHelper.HasDntInFilename(filePath);

        try
        {
            var doc = XDocument.Load(filePath, LoadOptions.SetLineInfo);
            var root = doc.Root;
            if (root is null)
                return entries;

            foreach (var dataElement in root.Elements("data"))
            {
                var entry = ParseDataElement(dataElement, locale, relativePath, isDntFile);
                if (entry is not null)
                    entries.Add(entry);
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Warning: Failed to parse {filePath}: {ex.Message}");
        }

        return entries;
    }

    private static RawLocalizedEntry? ParseDataElement(
        XElement dataElement, string locale, string relativePath, bool isDntFile)
    {
        var name = dataElement.Attribute("name")?.Value;
        if (string.IsNullOrEmpty(name))
            return null;

        // skip non-string entries (ex. binary/object data)
        if (dataElement.Attribute("type") is not null)
            return null;

        var valueElement = dataElement.Element("value");
        var value = valueElement?.Value ?? string.Empty;

        // preserve whitespace if xml:space="preserve"
        var xmlSpace = dataElement.Attribute(XNamespace.Xml + "space")?.Value;
        if (xmlSpace == "preserve" && valueElement is not null)
        {
            value = valueElement.Value;
        }

        var comment = dataElement.Element("comment")?.Value;

        int? line = null;
        if (dataElement is IXmlLineInfo lineInfo && lineInfo.HasLineInfo())
        {
            line = lineInfo.LineNumber;
        }

        return new RawLocalizedEntry
        {
            Key = name,
            Value = value,
            Locale = locale,
            Source = new SourceInfo
            {
                Format = "resx",
                File = relativePath,
                Path = relativePath,
                Line = line
            },
            Metadata = new EntryMetadata
            {
                Comment = comment,
                DoNotTranslate = isDntFile
            }
        };
    }

    internal static string DetectLocale(string filePath)
    {
        var fileName = Path.GetFileNameWithoutExtension(filePath);

        // Pattern: Messages.fr.resx → fr
        // Pattern: Messages.zh-CN.resx → zh-CN
        // Pattern: Messages.resx → en (base)
        var match = ResxLocalePattern().Match(fileName);
        if (match.Success)
        {
            var locale = match.Groups[1].Value;
            return NormalizeChineseLocale(locale);
        }

        return "en";
    }

    private static string NormalizeChineseLocale(string locale)
    {
        return locale.ToLowerInvariant() switch
        {
            "zh-hans" or "zh-chs" => "zh-CN",
            "zh-hant" or "zh-cht" => "zh-TW",
            _ => locale
        };
    }

    [System.Text.RegularExpressions.GeneratedRegex(@"\.([a-zA-Z]{2}(?:-[a-zA-Z0-9]+)?)$")]
    private static partial System.Text.RegularExpressions.Regex ResxLocalePattern();
}
