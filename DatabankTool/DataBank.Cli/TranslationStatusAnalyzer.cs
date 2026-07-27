using DataBank.Cli.Models;

namespace DataBank.Cli;

public static class TranslationStatusAnalyzer
{
    public static TranslationSummary Analyze(List<LocalizedStringEntry> entries)
    {
        var enKeys = new HashSet<string>(
            entries.Where(e => string.Equals(e.Locale, "en", StringComparison.OrdinalIgnoreCase))
                   .Select(e => e.Key),
            StringComparer.OrdinalIgnoreCase);

        foreach (var entry in entries)
        {
            if (entry.Metadata.DoNotTranslate)
            {
                entry.Metadata.IsTranslated = false;
                entry.Metadata.TranslationStatus = TranslationStatus.DoNotTranslate;
            }
            else if (string.Equals(entry.Locale, "en", StringComparison.OrdinalIgnoreCase))
            {
                entry.Metadata.IsTranslated = true;
                entry.Metadata.TranslationStatus = TranslationStatus.Translated;
            }
            else if (enKeys.Contains(entry.Key))
            {
                entry.Metadata.IsTranslated = true;
                entry.Metadata.TranslationStatus = TranslationStatus.Translated;
            }
            else
            {
                entry.Metadata.IsTranslated = false;
                entry.Metadata.TranslationStatus = TranslationStatus.NeedsReview;
            }
        }

        var summary = new TranslationSummary();

        var localeGroups = entries.GroupBy(e => e.Locale, StringComparer.OrdinalIgnoreCase);

        foreach (var group in localeGroups)
        {
            var locale = group.Key;
            var counts = new LocaleTranslationCounts { Locale = locale };

            foreach (var entry in group)
            {
                switch (entry.Metadata.TranslationStatus)
                {
                    case TranslationStatus.Translated:
                        counts.Translated++;
                        summary.Overall.Translated++;
                        break;
                    case TranslationStatus.Untranslated:
                        counts.Untranslated++;
                        summary.Overall.Untranslated++;
                        break;
                    case TranslationStatus.DoNotTranslate:
                        counts.DoNotTranslate++;
                        summary.Overall.DoNotTranslate++;
                        break;
                    case TranslationStatus.NeedsReview:
                        counts.NeedsReview++;
                        summary.Overall.NeedsReview++;
                        break;
                }
            }

            summary.ByLocale.Add(counts);
        }

        var targetLocales = entries.Where(e => !string.Equals(e.Locale, "en", StringComparison.OrdinalIgnoreCase))
                                   .Select(e => e.Locale)
                                   .Distinct(StringComparer.OrdinalIgnoreCase)
                                   .ToList();

        foreach (var locale in targetLocales)
        {
            var localeKeys = new HashSet<string>(
                entries.Where(e => string.Equals(e.Locale, locale, StringComparison.OrdinalIgnoreCase))
                       .Select(e => e.Key),
                StringComparer.OrdinalIgnoreCase);

            var missingCount = enKeys.Count(k => !localeKeys.Contains(k));

            if (missingCount > 0)
            {
                var localeCounts = summary.ByLocale.FirstOrDefault(lc =>
                    string.Equals(lc.Locale, locale, StringComparison.OrdinalIgnoreCase));

                if (localeCounts is not null)
                {
                    localeCounts.Untranslated += missingCount;
                }

                summary.Overall.Untranslated += missingCount;
            }
        }

        return summary;
    }
}
