using System.Text.RegularExpressions;
using DataBank.Cli.Helpers;
using DataBank.Cli.Models;

namespace DataBank.Cli.Parsers;

public static class FhxParser
{
    private static readonly Regex LangTagPattern = new(@"\blang(?:uage)?[:=]\s*(""([a-zA-Z\-]+)""|([a-zA-Z\-]+))", RegexOptions.Compiled);

    /// <summary>
    /// Parses an FHX file into raw localized string entries (one per key per locale).
    /// Use EntryGrouper.GroupByKey() to collapse into grouped entries.
    /// </summary>
    public static List<RawLocalizedEntry> Parse(string filePath, string? localeOverride = null, string? encodingOverride = null, string? rootDir = null)
    {
        var entries = new List<RawLocalizedEntry>();

        try
        {
            var content = EncodingDetector.ReadFile(filePath, encodingOverride);
            var relativePath = rootDir is not null
                ? Path.GetRelativePath(rootDir, filePath)
                : Path.GetFileName(filePath);

            var locale = DetectLocale(filePath, content, localeOverride);
            var isDntFile = FileHelper.HasDntInFilename(filePath);

            var lineNum = 0;
            foreach (var rawLine in content.Split(["\r\n", "\n", "\r"], StringSplitOptions.None))
            {
                lineNum++;
                var line = rawLine.TrimEnd('\r', '\n');
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var entry = ParseLine(line, locale, relativePath, isDntFile, lineNum);
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

    private static RawLocalizedEntry? ParseLine(string line, string locale, string relativePath, bool isDntFile, int lineNum)
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

        // File-level DNT takes precedence; otherwise check context-based detection
        var doNotTranslate = isDntFile || context.Contains("do NOT translate", StringComparison.OrdinalIgnoreCase);

        var metadata = new EntryMetadata
        {
            DoNotTranslate = doNotTranslate
        };

        RcParser.DetectFormatSpecifiers(value, metadata);

        return new RawLocalizedEntry
        {
            Key = key,
            Context = context,
            Value = value,
            Locale = locale,
            Source = new SourceInfo
            {
                Format = "fhx",
                File = relativePath,
                Path = relativePath,
                Line = lineNum
            },
            Metadata = metadata
        };
    }

    internal static string DetectLocale(string filePath, string content, string? localeOverride = null)
    {
        if (localeOverride is not null)
            return localeOverride;

        var pathLocale = DetectLocaleFromFilePath(filePath);
        if (pathLocale is not null)
            return pathLocale;

        var contentLocale = DetectLocaleFromContent(content);
        if (contentLocale is not null)
            return contentLocale;

        Console.Error.WriteLine($"Warning: Could not determine locale for {filePath}. Use --locale to specify.");
        return "unknown";
    }

    internal static string? DetectLocaleFromFilePath(string filePath)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (string.IsNullOrEmpty(directory))
            return null;

        var pathParts = directory.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        foreach (var part in pathParts)
        {
            var mapped = MapDirectoryNameToLocale(part);
            if (mapped is not null)
                return mapped;
        }

        return null;
    }

    internal static string? MapDirectoryNameToLocale(string dirName)
    {
        return dirName.ToUpperInvariant() switch
        {
            "EN" or "ENGLISH" => "en",
            "ZH-CN" or "ZHHANS" or "ZH-HANS" or "CHINESE" or "CHINESE (SIMPLIFIED)" => "zh-CN",
            "ZH-TW" or "ZH-HANT" or "CHINESE (TRADITIONAL)" => "zh-TW",
            "JA" or "JP" or "JPN" or "JAPANESE" => "ja",
            "KO" or "KOR" or "KOREAN" => "ko",
            "DE" or "DEU" or "GERMAN" => "de",
            "FR" or "FRA" or "FRENCH" => "fr",
            "ES" or "ESP" or "SPANISH" => "es",
            "PT" or "PTB" or "PORTUGUESE" or "PORTUGUESE (BRAZIL)" => "pt-BR",
            "RU" or "RUS" or "RUSSIAN" => "ru",
            "IT" or "ITA" or "ITALIAN" => "it",
            "NL" or "NLD" or "DUTCH" => "nl",
            "PL" or "PLK" or "POLISH" => "pl",
            "CS" or "CSY" or "CZECH" => "cs",
            "HU" or "HUN" or "HUNGARIAN" => "hu",
            "TR" or "TRK" or "TURKISH" => "tr",
            "LTK" => "lt",
            _ => null
        };
    }

    internal static string? DetectLocaleFromContent(string content)
    {
        var match = LangTagPattern.Match(content);
        if (!match.Success)
            return null;

        var langTag = match.Groups[1].Value;
        if (string.IsNullOrEmpty(langTag))
            return null;

        var normalized = NormalizeLangTag(langTag);
        if (normalized is not null)
            return normalized;

        Console.Error.WriteLine($"Warning: Found langtag \"{langTag}\" but could not map to BCP47 locale.");
        return null;
    }

    internal static string? NormalizeLangTag(string langTag)
    {
        var cleaned = langTag.Trim().ToLowerInvariant();
        var parts = cleaned.Split('-', 2);
        var lang = parts[0];

        return lang switch
        {
            "en" => "en",
            "zh" when parts.Length > 1 && parts[1] is "cn" or "chs" or "hans" => "zh-CN",
            "zh" when parts.Length > 1 && parts[1] is "tw" or "cht" or "hant" => "zh-TW",
            "zh" => "zh-CN",
            "ja" or "jp" or "jpn" => "ja",
            "ko" or "kor" => "ko",
            "de" or "deu" => "de",
            "fr" or "fra" => "fr",
            "es" or "esp" => "es",
            "pt" when parts.Length > 1 && parts[1] is "br" => "pt-BR",
            "pt" => "pt",
            "ru" or "rus" => "ru",
            "it" or "ita" => "it",
            "nl" or "nld" => "nl",
            "pl" or "plk" => "pl",
            "cs" or "csy" => "cs",
            "hu" or "hun" => "hu",
            "tr" or "trk" => "tr",
            _ => null
        };
    }
}
