using System.Text.RegularExpressions;
using DataBank.Cli.Models;

namespace DataBank.Cli.Helpers;

public static partial class CoverageAnalyzer
{
    public static CoverageReport Analyze(List<LocalizedStringEntry> allEntries, string rootDir)
    {
        var report = new CoverageReport();

        var enTranslatedPairs = FindEnTranslatedPairs(rootDir);

        if (enTranslatedPairs.Count == 0)
        {
            report.Summary.TotalUnmatchedFiles = 1;
            return report;
        }

        var localeStats = new Dictionary<string, (int enKeys, int translatedKeys)>(StringComparer.OrdinalIgnoreCase);

        foreach (var (enDir, translatedDir) in enTranslatedPairs)
        {
            var enFiles = Directory.GetFiles(enDir, "*.*", SearchOption.AllDirectories)
                .Where(f => IsSupportedFormat(f))
                .ToList();

            var translatedFiles = Directory.GetFiles(translatedDir, "*.*", SearchOption.AllDirectories)
                .Where(f => IsSupportedFormat(f))
                .ToList();

            var matchedPairs = MatchFilePairs(enFiles, translatedFiles, enDir, translatedDir);

            foreach (var (enFile, translatedFile) in matchedPairs)
            {
                var enRelativePath = GetRelativePath(enFile, rootDir);
                var transRelativePath = GetRelativePath(translatedFile, rootDir);

                // Find entries that have this file as a source for "en" locale
                var enEntries = allEntries.Where(e =>
                    e.Sources.ContainsKey("en") &&
                    e.Sources["en"].Path == enRelativePath).ToList();

                // Find entries that have this file as a source for any non-en locale
                var translatedEntries = allEntries.Where(e =>
                    e.Sources.Any(s =>
                        s.Key != "en" &&
                        s.Value.Path == transRelativePath)).ToList();

                var enKeys = enEntries.Select(e => e.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
                var translatedKeys = translatedEntries.Select(e => e.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);

                var missing = enKeys.Except(translatedKeys, StringComparer.OrdinalIgnoreCase).ToList();
                var orphaned = translatedKeys.Except(enKeys, StringComparer.OrdinalIgnoreCase).ToList();

                var locale = DetectLocaleFromEntries(translatedEntries);

                var completion = enKeys.Count > 0
                    ? (double)(enKeys.Count - missing.Count) / enKeys.Count * 100
                    : 100;

                var fileCoverage = new FileCoverage
                {
                    EnFile = enRelativePath,
                    TranslatedFile = transRelativePath,
                    Locale = locale,
                    EnKeyCount = enKeys.Count,
                    TranslatedKeyCount = translatedKeys.Count,
                    CompletionPercentage = Math.Round(completion, 1),
                    MissingKeys = missing,
                    OrphanedKeys = orphaned
                };

                report.Files.Add(fileCoverage);

                if (!localeStats.ContainsKey(locale))
                    localeStats[locale] = (0, 0);

                var stats = localeStats[locale];
                stats.enKeys += enKeys.Count;
                stats.translatedKeys += translatedKeys.Count - orphaned.Count;
                localeStats[locale] = stats;
            }
        }

        report.Summary.TotalEnKeys = report.Files.Sum(f => f.EnKeyCount);
        report.Summary.TotalTranslatedKeys = report.Files.Sum(f => f.TranslatedKeyCount);
        report.Summary.TotalMissingKeys = report.Files.Sum(f => f.MissingKeys.Count);
        report.Summary.TotalOrphanedKeys = report.Files.Sum(f => f.OrphanedKeys.Count);
        report.Summary.OverallCompletionPercentage = report.Summary.TotalEnKeys > 0
            ? Math.Round((double)(report.Summary.TotalEnKeys - report.Summary.TotalMissingKeys) / report.Summary.TotalEnKeys * 100, 1)
            : 100;

        foreach (var (locale, stats) in localeStats.OrderBy(kvp => kvp.Key))
        {
            report.Summary.ByLocale.Add(new LocaleCoverage
            {
                Locale = locale,
                EnKeys = stats.enKeys,
                TranslatedKeys = stats.translatedKeys,
                CompletionPercentage = stats.enKeys > 0
                    ? Math.Round((double)stats.translatedKeys / stats.enKeys * 100, 1)
                    : 100
            });
        }

        return report;
    }

    private static string DetectLocaleFromEntries(List<LocalizedStringEntry> translatedEntries)
    {
        if (translatedEntries.Count == 0)
            return "unknown";

        // Get locale from the first non-en value in the entry
        var entry = translatedEntries.First();
        var nonEnLocale = entry.Values.FirstOrDefault(v => v.Locale != "en")?.Locale;
        return nonEnLocale ?? "unknown";
    }

    private static List<(string enDir, string translatedDir)> FindEnTranslatedPairs(string rootDir)
    {
        var pairs = new List<(string, string)>();

        foreach (var dir in Directory.GetDirectories(rootDir))
        {
            var enChild = Path.Combine(dir, "EN");
            var transChild = Path.Combine(dir, "Translated");

            if (Directory.Exists(enChild) && Directory.Exists(transChild))
            {
                pairs.Add((enChild, transChild));
            }
        }

        var rootEn = Path.Combine(rootDir, "EN");
        var rootTrans = Path.Combine(rootDir, "Translated");
        if (Directory.Exists(rootEn) && Directory.Exists(rootTrans))
        {
            pairs.Add((rootEn, rootTrans));
        }

        return pairs;
    }

    private static bool IsSupportedFormat(string filePath)
    {
        return FileDetector.DetectFormat(filePath) is not null;
    }

    private static Dictionary<string, string> MatchFilePairs(
        List<string> enFiles, List<string> translatedFiles,
        string enDir, string translatedDir)
    {
        var pairs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var enFile in enFiles)
        {
            var relPath = GetRelativePath(enFile, enDir);
            var candidate = Path.Combine(translatedDir, relPath);

            // Exact match first
            var match = translatedFiles.FirstOrDefault(tf =>
                string.Equals(tf, candidate, StringComparison.OrdinalIgnoreCase));

            if (match is null)
            {
                // Try locale-suffixed match: Strings.resx -> Strings.zh-CN.resx
                var enBaseName = Path.GetFileNameWithoutExtension(enFile);
                var enExt = Path.GetExtension(enFile);

                match = translatedFiles.FirstOrDefault(tf =>
                {
                    var transName = Path.GetFileNameWithoutExtension(tf);
                    var transExt = Path.GetExtension(tf);
                    if (!string.Equals(transExt, enExt, StringComparison.OrdinalIgnoreCase))
                        return false;

                    // Check if translated name starts with en base name
                    if (!transName.StartsWith(enBaseName, StringComparison.OrdinalIgnoreCase))
                        return false;

                    // The suffix should be a locale code (e.g., ".zh-CN", ".fr")
                    var suffix = transName[enBaseName.Length..];
                    return suffix.Length > 0 && LocaleSuffixPattern().IsMatch(suffix);
                });
            }

            if (match is not null)
            {
                pairs[enFile] = match;
            }
        }

        return pairs;
    }

    private static string GetRelativePath(string fullPath, string basePath)
    {
        var baseUri = new Uri(basePath.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar);
        var fullUri = new Uri(fullPath);
        return Uri.UnescapeDataString(baseUri.MakeRelativeUri(fullUri).ToString()
            .Replace('/', Path.DirectorySeparatorChar));
    }

    [GeneratedRegex(@"^[.\-][a-zA-Z]{2}(?:-[a-zA-Z0-9]+)?$")]
    private static partial Regex LocaleSuffixPattern();
}
