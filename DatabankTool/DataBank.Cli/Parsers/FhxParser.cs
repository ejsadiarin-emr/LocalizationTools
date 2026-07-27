using System.Globalization;
using System.Text.RegularExpressions;
using DataBank.Cli.Helpers;
using DataBank.Cli.Models;

namespace DataBank.Cli.Parsers;

public static class FhxParser
{
    private static readonly Regex LangTagPattern = new(@"\blang(?:uage)?[:=]\s*(""([a-zA-Z\-]+)""|([a-zA-Z\-]+))", RegexOptions.Compiled);

    /// <summary>
    /// Parses an FHX file into localized string entries.
    /// Locale detection order: --locale override → parent directory name → langtag in content → "unknown".
    /// Use --locale when the directory name is not a valid BCP47 locale (e.g., "Translated").
    /// </summary>
    public static List<LocalizedStringEntry> Parse(string filePath, string? localeOverride = null, string? encodingOverride = null, string? rootDir = null)
    {
        var entries = new List<LocalizedStringEntry>();

        try
        {
            var content = EncodingDetector.ReadFile(filePath, encodingOverride);
            var relativePath = rootDir is not null
                ? Path.GetRelativePath(rootDir, filePath)
                : Path.GetFileName(filePath);

            var locale = DetectLocale(filePath, content, localeOverride);

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

    internal static string DetectLocale(string filePath, string content, string? localeOverride = null)
    {
        if (localeOverride is not null)
            return localeOverride;

        var parentDir = Path.GetFileName(Path.GetDirectoryName(filePath));

        if (!string.IsNullOrEmpty(parentDir))
        {
            var mapped = MapDirectoryNameToLocale(parentDir);
            if (mapped is not null)
                return mapped;
        }

        var contentLocale = DetectLocaleFromContent(content);
        if (contentLocale is not null)
            return contentLocale;

        Console.Error.WriteLine($"Warning: Could not determine locale for {filePath}. Use --locale to specify.");
        return "unknown";
    }

    internal static string? MapDirectoryNameToLocale(string dirName)
    {
        return dirName.ToUpperInvariant() switch
        {
            "EN" => "en",
            "ZH-CN" or "ZHHANS" or "ZH-HANS" => "zh-Hans",
            "ZH-TW" or "ZH-HANT" => "zh-Hant",
            "JA" or "JP" or "JPN" => "ja",
            "KO" or "KOR" => "ko",
            "DE" or "DEU" => "de",
            "FR" or "FRA" => "fr",
            "ES" or "ESP" => "es",
            "PT" or "PTB" => "pt-BR",
            "RU" or "RUS" => "ru",
            "IT" or "ITA" => "it",
            "NL" or "NLD" => "nl",
            "PL" or "PLK" => "pl",
            "CS" or "CSY" => "cs",
            "HU" or "HUN" => "hu",
            "TR" or "TRK" => "tr",
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
            "zh" when parts.Length > 1 && parts[1] is "cn" or "chs" or "hans" => "zh-Hans",
            "zh" when parts.Length > 1 && parts[1] is "tw" or "cht" or "hant" => "zh-Hant",
            "zh" => "zh-Hans",
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
