using DataBank.Api.Repositories;

namespace DataBank.Api.Endpoints;

public static class ExportEndpoints
{
    public static void MapExportEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/databank")
            .WithTags("DataBank Export");

        group.MapGet("/export", async (IDataBankRepository repository) =>
        {
            var metadata = await repository.GetMetadataAsync();
            var entries = await repository.GetAllEntriesAsync();
            var statusCounts = await repository.GetTranslationStatusCountsAsync();

            var translatedKeys = (int)statusCounts.GetValueOrDefault("Translated", 0);
            var untranslatedKeys = (int)statusCounts.GetValueOrDefault("Untranslated", 0);
            var doNotTranslateKeys = (int)statusCounts.GetValueOrDefault("DoNotTranslate", 0);
            var totalKeys = entries.Count;
            var completionPercentage = totalKeys > 0
                ? Math.Round((double)translatedKeys / totalKeys * 100, 1)
                : 0;

            var output = new
            {
                version = metadata?.Version ?? 3,
                generated = metadata?.Generated ?? DateTime.UtcNow.ToString("o"),
                entries = entries.Select(e => new
                {
                    id = e.Id,
                    key = e.Key,
                    values = e.Values.Select(v => new { locale = v.Locale, value = v.Value }).ToList(),
                    sources = e.Sources.ToDictionary(
                        kvp => kvp.Key,
                        kvp => new { format = kvp.Value.Format, file = kvp.Value.File, path = kvp.Value.Path, line = kvp.Value.Line }),
                    metadata = new
                    {
                        comment = e.Metadata.Comment,
                        formatSpecifiers = e.Metadata.FormatSpecifiers,
                        doNotTranslate = e.Metadata.DoNotTranslate,
                        isTranslated = e.Metadata.IsTranslated
                    }
                }).ToList(),
                translationSummary = new
                {
                    totalKeys,
                    translatedKeys,
                    untranslatedKeys,
                    doNotTranslateKeys,
                    completionPercentage
                }
            };

            return Results.Ok(output);
        })
        .WithName("ExportDataBank")
        .WithDescription("Export full DataBankOutput JSON matching CLI output format");
    }
}
