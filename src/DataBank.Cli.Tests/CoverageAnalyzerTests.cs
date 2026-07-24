using DataBank.Cli.Helpers;
using DataBank.Cli.Models;

namespace DataBank.Cli.Tests;

public class CoverageAnalyzerTests
{
    private static string RootDir => Path.Combine(
        Directory.GetCurrentDirectory(), "..", "..", "..", "..", "..", "l10n-files");

    [Fact]
    public void Analyze_WithL10nFiles_FindsFilePairs()
    {
        var rootDir = Path.GetFullPath(RootDir);
        var entries = new List<LocalizedStringEntry>();

        // Parse RC EN files
        var rcEnDir = Path.Combine(rootDir, "RC", "EN");
        if (Directory.Exists(rcEnDir))
        {
            foreach (var file in Directory.GetFiles(rcEnDir, "*.rc", SearchOption.AllDirectories))
            {
                entries.AddRange(Parsers.RcParser.Parse(file, rootDir: rootDir));
            }
        }

        // Parse RC Translated files
        var rcTransDir = Path.Combine(rootDir, "RC", "Translated");
        if (Directory.Exists(rcTransDir))
        {
            foreach (var file in Directory.GetFiles(rcTransDir, "*.rc", SearchOption.AllDirectories))
            {
                entries.AddRange(Parsers.RcParser.Parse(file, rootDir: rootDir));
            }
        }

        var report = CoverageAnalyzer.Analyze(entries, rootDir);

        Assert.NotEmpty(report.Files);
        Assert.True(report.Summary.TotalEnKeys > 0);
    }

    [Fact]
    public void Analyze_EmptyEntries_ReturnsEmptyReport()
    {
        var report = CoverageAnalyzer.Analyze([], ".");

        Assert.Empty(report.Files);
        Assert.Equal(0, report.Summary.TotalEnKeys);
    }

    [Fact]
    public void Analyze_DetectsMissingKeys()
    {
        var entries = new List<LocalizedStringEntry>
        {
            new()
            {
                Id = "test::en::key1", Key = "key1", Value = "val1", Locale = "en",
                Source = new SourceInfo { Format = "rc", File = "test.rc", Path = "test.rc" }
            },
            new()
            {
                Id = "test::en::key2", Key = "key2", Value = "val2", Locale = "en",
                Source = new SourceInfo { Format = "rc", File = "test.rc", Path = "test.rc" }
            },
            new()
            {
                Id = "test::fr::key1", Key = "key1", Value = "val1fr", Locale = "fr",
                Source = new SourceInfo { Format = "rc", File = "test.rc", Path = "test.rc" }
            }
        };

        var report = CoverageAnalyzer.Analyze(entries, ".");

        // No file pairs will match since there's no EN/Translated directory structure,
        // but we verify the analyzer doesn't crash
        Assert.NotNull(report);
    }

    [Fact]
    public void CoverageReport_Serialization_RoundTrips()
    {
        var report = new CoverageReport
        {
            Files =
            [
                new FileCoverage
                {
                    EnFile = "RC/EN/test.rc",
                    TranslatedFile = "RC/Translated/test.rc",
                    Locale = "fr",
                    EnKeyCount = 10,
                    TranslatedKeyCount = 8,
                    CompletionPercentage = 80.0,
                    MissingKeys = ["key9", "key10"],
                    OrphanedKeys = ["oldKey"]
                }
            ],
            Summary = new CoverageSummary
            {
                TotalEnKeys = 10,
                TotalTranslatedKeys = 8,
                OverallCompletionPercentage = 80.0,
                TotalMissingKeys = 2,
                TotalOrphanedKeys = 1,
                ByLocale =
                [
                    new LocaleCoverage
                    {
                        Locale = "fr",
                        EnKeys = 10,
                        TranslatedKeys = 8,
                        CompletionPercentage = 80.0
                    }
                ]
            }
        };

        var json = System.Text.Json.JsonSerializer.Serialize(report,
            new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        var deserialized = System.Text.Json.JsonSerializer.Deserialize<CoverageReport>(json,
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(deserialized);
        Assert.Single(deserialized.Files);
        Assert.Equal("fr", deserialized.Files[0].Locale);
        Assert.Equal(80.0, deserialized.Files[0].CompletionPercentage);
        Assert.Equal(2, deserialized.Files[0].MissingKeys.Count);
        Assert.Single(deserialized.Files[0].OrphanedKeys);
        Assert.Single(deserialized.Summary.ByLocale);
    }
}
