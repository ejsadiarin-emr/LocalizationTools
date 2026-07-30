using DataBank.Cli.Models;

namespace DataBank.Cli;

public static class TranslationStatusAnalyzer
{
    public static TranslationSummary Analyze(List<LocalizedStringEntry> entries)
    {
        var summary = new TranslationSummary();

        foreach (var entry in entries)
        {
            var status = entry.Metadata.GetDerivedStatus();

            // Count per-locale status
            foreach (var val in entry.Values)
            {
                var localeCounts = summary.ByLocale.FirstOrDefault(lc =>
                    string.Equals(lc.Locale, val.Locale, StringComparison.OrdinalIgnoreCase));

                if (localeCounts is null)
                {
                    localeCounts = new LocaleTranslationCounts { Locale = val.Locale };
                    summary.ByLocale.Add(localeCounts);
                }

                // For each locale value, determine if it's translated
                if (entry.Metadata.DoNotTranslate)
                {
                    localeCounts.DoNotTranslate++;
                    summary.Overall.DoNotTranslate++;
                }
                else if (!string.IsNullOrEmpty(val.Value))
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

            // If entry has no values at all, count as untranslated
            if (entry.Values.Count == 0)
            {
                summary.Overall.Untranslated++;
            }
        }

        return summary;
    }
}
