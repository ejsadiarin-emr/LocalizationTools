using System.Text.RegularExpressions;
using DataBank.Cli.Helpers;
using DataBank.Cli.Models;

namespace DataBank.Cli.Parsers;

public static partial class RcParser
{
    public static List<LocalizedStringEntry> Parse(string filePath, Dictionary<int, string>? symbolMap = null, string? rootDir = null, string? encodingOverride = null)
    {
        var entries = new List<LocalizedStringEntry>();
        var relativePath = rootDir is not null
            ? Path.GetRelativePath(rootDir, filePath)
            : Path.GetFileName(filePath);

        var encodingName = encodingOverride ?? EncodingDetector.Detect(filePath).WebName;

        try
        {
            var content = EncodingDetector.ReadFile(filePath, encodingOverride);
            var lines = NormalizeContent(content);

            var currentLocale = "en";
            var inStringTable = false;
            var inDialog = false;
            var inDesignInfo = false;

            foreach (var rawLine in lines)
            {
                var line = rawLine.Trim();

                // Skip APSTUDIO_INVOKED blocks (DESIGNINFO, TEXTINCLUDE)
                if (line.StartsWith("#ifdef APSTUDIO_INVOKED", StringComparison.OrdinalIgnoreCase))
                {
                    inDesignInfo = true;
                    continue;
                }
                if (inDesignInfo)
                {
                    if (line.StartsWith("#endif", StringComparison.OrdinalIgnoreCase))
                        inDesignInfo = false;
                    continue;
                }

                // Track LANGUAGE directives
                var langMatch = LanguagePattern().Match(line);
                if (langMatch.Success)
                {
                    currentLocale = MapLanguageToLocale(
                        langMatch.Groups[1].Value,
                        langMatch.Groups[2].Value);
                    continue;
                }

                // STRINGTABLE blocks
                if (!inDialog && line.StartsWith("STRINGTABLE", StringComparison.OrdinalIgnoreCase))
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

                if (inStringTable)
                {
                    var entry = ParseStringEntry(line, currentLocale, relativePath, symbolMap, encodingName);
                    if (entry is not null)
                        entries.Add(entry);
                    continue;
                }

                // DIALOGEX/DIALOG blocks
                if (!inStringTable && DialogStartPattern().IsMatch(line))
                {
                    inDialog = true;

                    // Check for CAPTION on the same line as DIALOGEX
                    var captionMatch = DialogCaptionPattern().Match(line);
                    if (captionMatch.Success)
                    {
                        var captionValue = UnescapeValue(captionMatch.Groups[1].Value);
                        if (captionValue.Length > 0)
                        {
                            entries.Add(CreateDialogEntry(
                                captionValue, currentLocale, relativePath,
                                "CAPTION", encodingName));
                        }
                    }
                    continue;
                }

                if (inDialog && line == "BEGIN")
                    continue;

                if (inDialog && line == "END")
                {
                    inDialog = false;
                    continue;
                }

                if (inDialog)
                {
                    // CAPTION on its own line
                    if (line.StartsWith("CAPTION", StringComparison.OrdinalIgnoreCase))
                    {
                        var captionValue = ExtractQuotedString(line, "CAPTION".Length);
                        if (captionValue is not null && captionValue.Length > 0)
                        {
                            entries.Add(CreateDialogEntry(
                                captionValue, currentLocale, relativePath,
                                "CAPTION", encodingName));
                        }
                        continue;
                    }

                    // Extract strings from control elements
                    var controlEntry = ParseDialogControl(line, currentLocale, relativePath, symbolMap, encodingName);
                    if (controlEntry is not null)
                        entries.Add(controlEntry);
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Warning: Failed to parse RC file {filePath}: {ex.Message}");
        }

        return entries;
    }

    private static LocalizedStringEntry CreateDialogEntry(
        string value, string locale, string relativePath,
        string controlType, string encodingName)
    {
        var key = $"{controlType}::{relativePath}::{value}";
        var metadata = new EntryMetadata();
        DetectFormatSpecifiers(value, metadata);

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
                Path = relativePath,
                Encoding = encodingName
            },
            Metadata = metadata
        };
    }

    private static LocalizedStringEntry? ParseDialogControl(
        string line, string locale, string relativePath,
        Dictionary<int, string>? symbolMap, string encodingName)
    {
        // LTEXT "text",IDC_xxx,x,y,w,h
        // PUSHBUTTON "text",IDC_xxx,x,y,w,h
        // DEFPUSHBUTTON "text",IDC_xxx,x,y,w,h
        // GROUPBOX "text",IDC_xxx,x,y,w,h
        // CTEXT "text",IDC_xxx,x,y,w,h
        var textMatch = TextControlPattern().Match(line);
        if (textMatch.Success)
        {
            var value = UnescapeValue(textMatch.Groups[2].Value);
            if (value.Length == 0)
                return null;

            var controlType = textMatch.Groups[1].Value.ToUpperInvariant();
            var idPart = textMatch.Groups[3].Value;
            var (numericId, defineName) = ResolveId(idPart, symbolMap);
            var key = $"{controlType}::{relativePath}::{defineName ?? idPart}";

            var metadata = new EntryMetadata
            {
                RcId = numericId,
                RcDefine = defineName
            };
            DetectFormatSpecifiers(value, metadata);

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
                    Path = relativePath,
                    Encoding = encodingName
                },
                Metadata = metadata
            };
        }

        // CONTROL "text",IDC_xxx,"Class",style,x,y,w,h
        var controlMatch = ControlPattern().Match(line);
        if (controlMatch.Success)
        {
            var value = UnescapeValue(controlMatch.Groups[1].Value);
            if (value.Length == 0)
                return null;

            var idPart = controlMatch.Groups[2].Value;
            var (numericId, defineName) = ResolveId(idPart, symbolMap);
            var key = $"CONTROL::{relativePath}::{defineName ?? idPart}";

            var metadata = new EntryMetadata
            {
                RcId = numericId,
                RcDefine = defineName
            };
            DetectFormatSpecifiers(value, metadata);

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
                    Path = relativePath,
                    Encoding = encodingName
                },
                Metadata = metadata
            };
        }

        return null;
    }

    private static (int numericId, string? defineName) ResolveId(string idPart, Dictionary<int, string>? symbolMap)
    {
        int numericId;
        string? defineName = null;

        if (int.TryParse(idPart, out numericId))
        {
            if (symbolMap is not null && symbolMap.TryGetValue(numericId, out var resolved))
                defineName = resolved;
        }
        else
        {
            defineName = idPart;
            if (symbolMap is not null)
            {
                var reverseEntry = symbolMap.FirstOrDefault(e =>
                    string.Equals(e.Value, idPart, StringComparison.OrdinalIgnoreCase));
                if (reverseEntry.Key != 0)
                    numericId = reverseEntry.Key;
            }
        }

        return (numericId, defineName);
    }

    private static string? ExtractQuotedString(string line, int startIndex)
    {
        var quoteStart = line.IndexOf('"', startIndex);
        if (quoteStart < 0)
            return null;

        var quoteEnd = line.LastIndexOf('"');
        if (quoteEnd <= quoteStart)
            return null;

        return line[(quoteStart + 1)..quoteEnd];
    }

    internal static void DetectFormatSpecifiers(string value, EntryMetadata metadata)
    {
        var matches = FormatSpecifierPattern().Matches(value);
        if (matches.Count == 0)
            return;

        var specifiers = new List<string>();
        foreach (Match match in matches)
        {
            // Skip literal %%
            if (match.Value == "%%")
                continue;
            specifiers.Add(match.Value);
        }

        if (specifiers.Count > 0)
        {
            metadata.IsBehavioral = true;
            metadata.FormatSpecifiers.AddRange(specifiers);
        }
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
        Dictionary<int, string>? symbolMap, string encodingName)
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

        var metadata = new EntryMetadata
        {
            RcId = numericId,
            RcDefine = defineName
        };
        DetectFormatSpecifiers(value, metadata);

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
                Path = relativePath,
                Encoding = encodingName
            },
            Metadata = metadata
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
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Warning: Failed to parse resource.h {filePath}: {ex.Message}");
        }

        return map;
    }

    [GeneratedRegex(@"^\s*(\w+)\s+""(.*)""\s*$")]
    private static partial Regex StringEntryPattern();

    [GeneratedRegex(@"LANGUAGE\s+(\w+)\s*,\s*(\w+)")]
    private static partial Regex LanguagePattern();

    [GeneratedRegex(@"#define\s+(\w+)\s+(\d+)")]
    private static partial Regex DefinePattern();

    [GeneratedRegex(@"^\s*\w+\s+DIALOG(EX)?\s")]
    private static partial Regex DialogStartPattern();

    [GeneratedRegex(@"CAPTION\s+""([^""]*)""")]
    private static partial Regex DialogCaptionPattern();

    [GeneratedRegex(@"^\s*(LTEXT|PUSHBUTTON|DEFPUSHBUTTON|GROUPBOX|CTEXT)\s+""([^""]*)"",\s*(\w+)")]
    private static partial Regex TextControlPattern();

    [GeneratedRegex(@"^\s*CONTROL\s+""([^""]*)"",\s*(\w+)\s*,")]
    private static partial Regex ControlPattern();

    [GeneratedRegex(@"%(?:%|[-+ #0]*(?:\d+|\*)?(?:\.(?:\d+|\*))?[hlLqjzt]*[diouxXeEfFgGaAcspn])")]
    private static partial Regex FormatSpecifierPattern();
}
