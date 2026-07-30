using DataBank.Cli.Helpers;
using DataBank.Cli.Models;

namespace DataBank.Cli.Parsers;

public static partial class GrfParser
{
    public static List<RawLocalizedEntry> Parse(string filePath, string? rootDir = null)
    {
        var entries = new List<RawLocalizedEntry>();
        var relativePath = rootDir is not null
            ? Path.GetRelativePath(rootDir, filePath)
            : Path.GetFileName(filePath);

        var locale = DetectLocale(filePath);
        var fileName = Path.GetFileNameWithoutExtension(filePath);

        entries.Add(new RawLocalizedEntry
        {
            Key = fileName,
            Value = $"GRF file: {Path.GetFileName(filePath)}",
            Locale = locale,
            Source = new SourceInfo
            {
                Format = "grf",
                File = relativePath,
                Path = relativePath
            },
            Metadata = new EntryMetadata
            {
                Comment = "GRF file - not parsed, listed for reference only"
            }
        });

        return entries;
    }

    internal static string DetectLocale(string filePath)
    {
        var pathLocale = FhxParser.DetectLocaleFromFilePath(filePath);
        if (pathLocale is not null)
            return pathLocale;

        var fileLocale = DetectLocaleFromFileName(filePath);
        if (fileLocale is not null)
            return fileLocale;

        return "unknown";
    }

    internal static string? DetectLocaleFromFileName(string filePath)
    {
        var fileName = Path.GetFileNameWithoutExtension(filePath);
        var match = GrfLocalePattern().Match(fileName);
        if (!match.Success)
            return null;

        var locale = match.Groups[1].Value;
        return NormalizeLocale(locale);
    }

    private static string NormalizeLocale(string locale)
    {
        return locale.ToLowerInvariant() switch
        {
            "zh-hans" or "zh-chs" => "zh-CN",
            "zh-hant" or "zh-cht" => "zh-TW",
            _ => locale
        };
    }

    [System.Text.RegularExpressions.GeneratedRegex(@"\.([a-zA-Z]{2}(?:-[a-zA-Z0-9]+)?)$")]
    private static partial System.Text.RegularExpressions.Regex GrfLocalePattern();
}
