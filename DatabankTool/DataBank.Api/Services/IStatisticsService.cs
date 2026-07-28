using DataBank.Cli.Models;

namespace DataBank.Api.Services;

public interface IStatisticsService
{
    StatisticsResult GetStatistics();
    CoverageReport GetCoverage(string? locale = null, string? format = null);
}

public class StatisticsResult
{
    public int TotalEntries { get; set; }
    public int UniqueKeys { get; set; }
    public Dictionary<string, int> ByLocale { get; set; } = new();
    public Dictionary<string, int> ByFormat { get; set; } = new();
    public TranslationStatusBreakdown TranslationStatus { get; set; } = new();
}

public class TranslationStatusBreakdown
{
    public int Translated { get; set; }
    public int Untranslated { get; set; }
    public int DoNotTranslate { get; set; }
    public int NeedsReview { get; set; }
}
