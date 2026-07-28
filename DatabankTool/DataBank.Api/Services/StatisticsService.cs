using DataBank.Cli.Models;

namespace DataBank.Api.Services;

public class StatisticsService : IStatisticsService
{
    private readonly IDataBankService _dataBankService;

    public StatisticsService(IDataBankService dataBankService)
    {
        _dataBankService = dataBankService;
    }

    public StatisticsResult GetStatistics()
    {
        var entries = _dataBankService.GetAllEntries();

        var stats = new StatisticsResult
        {
            TotalEntries = entries.Count,
            UniqueKeys = entries.Select(e => e.Key).Distinct().Count()
        };

        stats.ByLocale = entries
            .GroupBy(e => e.Locale)
            .ToDictionary(g => g.Key, g => g.Count());

        stats.ByFormat = entries
            .GroupBy(e => e.Source.Format)
            .ToDictionary(g => g.Key, g => g.Count());

        stats.TranslationStatus = new TranslationStatusBreakdown
        {
            Translated = entries.Count(e => e.Metadata.TranslationStatus == TranslationStatus.Translated),
            Untranslated = entries.Count(e => e.Metadata.TranslationStatus == TranslationStatus.Untranslated),
            DoNotTranslate = entries.Count(e => e.Metadata.TranslationStatus == TranslationStatus.DoNotTranslate),
            NeedsReview = entries.Count(e => e.Metadata.TranslationStatus == TranslationStatus.NeedsReview)
        };

        return stats;
    }

    public CoverageReport GetCoverage(string? locale = null, string? format = null)
    {
        var entries = _dataBankService.GetAllEntries();

        if (locale is not null)
            entries = entries.Where(e => e.Locale == locale).ToList();

        if (format is not null)
            entries = entries.Where(e => e.Source.Format == format).ToList();

        var report = new CoverageReport();

        var byLocale = entries.GroupBy(e => e.Locale);
        foreach (var group in byLocale)
        {
            var total = group.Count();
            var translated = group.Count(e => e.Metadata.TranslationStatus == TranslationStatus.Translated);
            var percentage = total > 0 ? Math.Round((double)translated / total * 100, 1) : 0;

            report.Summary.ByLocale.Add(new LocaleCoverage
            {
                Locale = group.Key,
                EnKeys = total,
                TranslatedKeys = translated,
                CompletionPercentage = percentage
            });
        }

        report.Summary.TotalEnKeys = entries.Count;
        report.Summary.TotalTranslatedKeys = entries.Count(e => e.Metadata.TranslationStatus == TranslationStatus.Translated);
        report.Summary.OverallCompletionPercentage = entries.Count > 0
            ? Math.Round((double)report.Summary.TotalTranslatedKeys / entries.Count * 100, 1)
            : 0;

        return report;
    }
}
