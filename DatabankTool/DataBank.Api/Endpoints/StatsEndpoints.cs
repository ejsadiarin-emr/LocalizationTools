using DataBank.Api.Repositories;

namespace DataBank.Api.Endpoints;

public static class StatsEndpoints
{
    public static void MapStatsEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/stats")
            .WithTags("Statistics");

        group.MapGet("/", async (IDataBankRepository repository) =>
        {
            var totalCount = await repository.GetEntryCountAsync();
            var uniqueKeys = await repository.GetUniqueKeyCountAsync();
            var byLocale = await repository.GetEntryCountByLocaleAsync();
            var byFormat = await repository.GetEntryCountByFormatAsync();
            var statusCounts = await repository.GetTranslationStatusCountsAsync();

            var stats = new
            {
                TotalEntries = totalCount,
                UniqueKeys = uniqueKeys,
                ByLocale = byLocale,
                ByFormat = byFormat,
                TranslationStatus = new
                {
                    Translated = statusCounts.GetValueOrDefault("Translated", 0),
                    Untranslated = statusCounts.GetValueOrDefault("Untranslated", 0),
                    DoNotTranslate = statusCounts.GetValueOrDefault("DoNotTranslate", 0),
                    NeedsReview = statusCounts.GetValueOrDefault("NeedsReview", 0)
                }
            };

            return Results.Ok(stats);
        })
        .WithName("GetStatistics")
        .WithDescription("Get comprehensive localization statistics");

        group.MapGet("/coverage", async (
            IDataBankRepository repository,
            [Microsoft.AspNetCore.Mvc.FromQuery] string? locale = null,
            [Microsoft.AspNetCore.Mvc.FromQuery] string? format = null) =>
        {
            var totalCount = await repository.GetEntryCountAsync();
            var byLocaleStatus = await repository.GetTranslationStatusCountsByLocaleAsync();

            var coverageByLocale = new List<object>();
            var totalTranslated = 0L;

            foreach (var (loc, statusCounts) in byLocaleStatus)
            {
                if (!string.IsNullOrEmpty(locale) && loc != locale)
                    continue;

                var localeTotal = statusCounts.Values.Sum();
                var translated = statusCounts.GetValueOrDefault("Translated", 0);
                totalTranslated += translated;
                var percentage = localeTotal > 0 ? Math.Round((double)translated / localeTotal * 100, 1) : 0;

                coverageByLocale.Add(new
                {
                    Locale = loc,
                    TotalKeys = localeTotal,
                    TranslatedKeys = translated,
                    CompletionPercentage = percentage
                });
            }

            var overallPercentage = totalCount > 0
                ? Math.Round((double)totalTranslated / totalCount * 100, 1)
                : 0;

            var report = new
            {
                Summary = new
                {
                    TotalEnKeys = totalCount,
                    TotalTranslatedKeys = totalTranslated,
                    OverallCompletionPercentage = overallPercentage
                },
                ByLocale = coverageByLocale
            };

            return Results.Ok(report);
        })
        .WithName("GetCoverage")
        .WithDescription("Get coverage summary information");
    }
}
