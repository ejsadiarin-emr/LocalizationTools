using DataBank.Cli.Models;

namespace DataBank.Cli;

public static class TranslationStatusAnalyzer
{
    public static TranslationSummary Analyze(List<LocalizedStringEntry> entries)
    {
        var summary = new TranslationSummary();

        foreach (var entry in entries)
        {
            var enValue = entry.Values.FirstOrDefault(v => v.Locale == "en")?.Value;

            // Count per-locale status (skip EN — it's the source language, not a translation)
            foreach (var val in entry.Values)
            {
                if (string.Equals(val.Locale, "en", StringComparison.OrdinalIgnoreCase))
                    continue;

                var localeCounts = summary.ByLocale.FirstOrDefault(lc =>
                    string.Equals(lc.Locale, val.Locale, StringComparison.OrdinalIgnoreCase));

                if (localeCounts is null)
                {
                    localeCounts = new LocaleTranslationCounts { Locale = val.Locale };
                    summary.ByLocale.Add(localeCounts);
                }

                // For each non-EN locale value, determine if it's translated
                if (entry.Metadata.DoNotTranslate)
                {
                    localeCounts.DoNotTranslate++;
                    summary.Overall.DoNotTranslate++;
                }
                else if (!string.IsNullOrEmpty(val.Value) && val.Value != enValue)
                {
                    localeCounts.Translated++;
                    summary.Overall.Translated++;
                }
                else
                {
                    localeCounts.Untranslated++;
                    summary.Overall.Untranslated++;
                }
            }

            // If entry has no non-EN values at all, count DNT status or as untranslated
            if (entry.Values.All(v => string.Equals(v.Locale, "en", StringComparison.OrdinalIgnoreCase)))
            {
                if (entry.Metadata.DoNotTranslate)
                    summary.Overall.DoNotTranslate++;
                // EN-only entries with no other locales: not counted as translated or untranslated
            }
        }

        return summary;
    }
}
