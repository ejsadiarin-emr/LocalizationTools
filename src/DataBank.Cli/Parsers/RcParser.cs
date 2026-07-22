using System.Text.RegularExpressions;
using DataBank.Cli.Models;

namespace DataBank.Cli.Parsers;

public static partial class RcParser
{
    public static List<LocalizedStringEntry> Parse(string filePath, Dictionary<int, string>? symbolMap = null)
    {
        var entries = new List<LocalizedStringEntry>();
        var relativePath = Path.GetFileName(filePath);

        try
        {
            var content = File.ReadAllText(filePath);
            var lines = NormalizeContent(content);

            var currentLocale = "en";
            var inStringTable = false;

            foreach (var rawLine in lines)
            {
                var line = rawLine.Trim();

                // Track LANGUAGE directives
                var langMatch = LanguagePattern().Match(line);
                if (langMatch.Success)
                {
                    currentLocale = MapLanguageToLocale(
                        langMatch.Groups[1].Value,
                        langMatch.Groups[2].Value);
                    continue;
                }

                if (line.StartsWith("STRINGTABLE", StringComparison.OrdinalIgnoreCase))
                {
                    inStringTable = true;
                    continue;
                }

                if (inStringTable && line == "BEGIN")
                    continue;

                if (inStringTable && line == "END")
                {
                    inStringTable = false;
                    continue;
                }

                // Parse string entries inside STRINGTABLE
                if (inStringTable)
                {
                    var entry = ParseStringEntry(line, currentLocale, relativePath, symbolMap);
                    if (entry is not null)
                        entries.Add(entry);
                }
            }
        }
        catch (Exception)
        {
            // Gracefully handle file read errors
        }

        return entries;
    }

    private static List<string> NormalizeContent(string content)
    {
        var lines = new List<string>();
        var remaining = content;

        while (remaining.Length > 0)
        {
            var newlineIndex = remaining.IndexOf('\n');
            string line;
            if (newlineIndex >= 0)
            {
                line = remaining[..newlineIndex];
                remaining = remaining[(newlineIndex + 1)..];
            }
            else
            {
                line = remaining;
                remaining = string.Empty;
            }

            // Handle line continuations
            while (line.TrimEnd().EndsWith('\\') && remaining.Length > 0)
            {
                line = line.TrimEnd();
                line = line[..^1]; // Remove trailing backslash

                var nextNewline = remaining.IndexOf('\n');
                string nextLine;
                if (nextNewline >= 0)
                {
                    nextLine = remaining[..nextNewline];
                    remaining = remaining[(nextNewline + 1)..];
                }
                else
                {
                    nextLine = remaining;
                    remaining = string.Empty;
                }

                line += nextLine.TrimStart();
            }

            lines.Add(line);
        }

        return lines;
    }

    private static LocalizedStringEntry? ParseStringEntry(
        string line, string locale, string relativePath,
        Dictionary<int, string>? symbolMap)
    {
        // Match: IDS_WELCOME "Welcome" or 100 "Welcome"
        var match = StringEntryPattern().Match(line);
        if (!match.Success)
            return null;

        var idPart = match.Groups[1].Value;
        var value = UnescapeValue(match.Groups[2].Value);

        int numericId;
        string? defineName = null;

        if (int.TryParse(idPart, out numericId))
        {
            // Numeric ID — try to resolve symbol
            if (symbolMap is not null && symbolMap.TryGetValue(numericId, out var resolved))
                defineName = resolved;
        }
        else
        {
            // Symbolic ID — try to find numeric ID from symbolMap (reverse lookup)
            defineName = idPart;
            if (symbolMap is not null)
            {
                var reverseEntry = symbolMap.FirstOrDefault(e =>
                    string.Equals(e.Value, idPart, StringComparison.OrdinalIgnoreCase));
                if (reverseEntry.Key != 0)
                    numericId = reverseEntry.Key;
            }
        }

        var key = defineName ?? idPart.ToString();

        return new LocalizedStringEntry
        {
            Id = $"rc::{relativePath}::{key}",
            Key = key,
            Value = value,
            Locale = locale,
            Source = new SourceInfo
            {
                Format = "rc",
                File = relativePath,
                Path = relativePath
            },
            Metadata = new EntryMetadata
            {
                RcId = numericId,
                RcDefine = defineName
            }
        };
    }

    internal static string UnescapeValue(string value)
    {
        // Strip L prefix for Unicode strings
        if (value.StartsWith('L'))
            value = value[1..];

        // Remove surrounding quotes
        if (value.Length >= 2 && value.StartsWith('"') && value.EndsWith('"'))
            value = value[1..^1];

        // Unescape "" → "
        value = value.Replace("\"\"", "\"");

        return value;
    }

    internal static string MapLanguageToLocale(string lang, string sublang)
    {
        return (lang.ToUpperInvariant(), sublang.ToUpperInvariant()) switch
        {
            ("LANG_ENGLISH", _) => "en",
            ("LANG_FRENCH", _) => "fr",
            ("LANG_GERMAN", _) => "de",
            ("LANG_SPANISH", _) => "es",
            ("LANG_ITALIAN", _) => "it",
            ("LANG_PORTUGUESE", _) => "pt",
            ("LANG_RUSSIAN", _) => "ru",
            ("LANG_JAPANESE", _) => "ja",
            ("LANG_CHINESE", "SUBLANG_CHINESE_SIMPLIFIED") => "zh-Hans",
            ("LANG_CHINESE", "SUBLANG_CHINESE_TRADITIONAL") => "zh-Hant",
            ("LANG_CHINESE", _) => "zh",
            ("LANG_KOREAN", _) => "ko",
            ("LANG_ARABIC", _) => "ar",
            ("LANG_DUTCH", _) => "nl",
            ("LANG_SWEDISH", _) => "sv",
            ("LANG_POLISH", _) => "pl",
            ("LANG_TURKISH", _) => "tr",
            _ => lang.ToLowerInvariant()
        };
    }

    public static Dictionary<int, string> ParseResourceH(string filePath)
    {
        var map = new Dictionary<int, string>();

        try
        {
            var content = File.ReadAllText(filePath);
            foreach (var match in DefinePattern().Matches(content).Cast<Match>())
            {
                var symbol = match.Groups[1].Value;
                if (int.TryParse(match.Groups[2].Value, out var id))
                {
                    map[id] = symbol;
                }
            }
        }
        catch (Exception)
        {
            // Gracefully handle errors
        }

        return map;
    }

    [GeneratedRegex(@"^\s*(\w+)\s+""(.*)""\s*$")]
    private static partial Regex StringEntryPattern();

    [GeneratedRegex(@"LANGUAGE\s+(\w+)\s*,\s*(\w+)")]
    private static partial Regex LanguagePattern();

    [GeneratedRegex(@"#define\s+(\w+)\s+(\d+)")]
    private static partial Regex DefinePattern();
}
