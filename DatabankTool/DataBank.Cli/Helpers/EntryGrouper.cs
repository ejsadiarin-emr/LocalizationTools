using DataBank.Cli.Models;

namespace DataBank.Cli.Helpers;

public static class EntryGrouper
{
    /// <summary>
    /// Groups flat raw entries by key+context, merging values and sources per locale.
    /// For FHX entries, context distinguishes same keys in different files.
    /// For other formats (context is null), grouping is by key alone.
    /// </summary>
    public static List<LocalizedStringEntry> GroupByKey(List<RawLocalizedEntry> flatEntries)
    {
        var grouped = new Dictionary<string, LocalizedStringEntry>();

        foreach (var entry in flatEntries)
        {
            var groupKey = GetGroupingKey(entry.Key, entry.Context);

            if (!grouped.TryGetValue(groupKey, out var existing))
            {
                var id = string.IsNullOrEmpty(entry.Context)
                    ? entry.Key
                    : $"{entry.Key}|||{entry.Context}";

                existing = new LocalizedStringEntry
                {
                    Id = id,
                    Key = entry.Key,
                    Context = entry.Context,
                    Values = [],
                    Sources = new Dictionary<string, SourceInfo>(),
                    Metadata = new EntryMetadata
                    {
                        Comment = entry.Metadata.Comment,
                        FormatSpecifiers = new List<string>(entry.Metadata.FormatSpecifiers),
                        DoNotTranslate = entry.Metadata.DoNotTranslate,
                        IsTranslated = entry.Metadata.IsTranslated
                    }
                };
                grouped[groupKey] = existing;
            }

            // Add locale value if not already present
            if (!existing.Values.Any(v => v.Locale == entry.Locale))
            {
                existing.Values.Add(new LocaleValue
                {
                    Locale = entry.Locale,
                    Value = entry.Value
                });
            }

            // Add source for this locale if not already present
            if (!existing.Sources.ContainsKey(entry.Locale))
            {
                existing.Sources[entry.Locale] = entry.Source;
            }

            // Merge metadata: keep the most permissive settings
            if (entry.Metadata.DoNotTranslate)
                existing.Metadata.DoNotTranslate = true;
            if (entry.Metadata.IsTranslated)
                existing.Metadata.IsTranslated = true;
            if (entry.Metadata.Comment is not null && existing.Metadata.Comment is null)
                existing.Metadata.Comment = entry.Metadata.Comment;
            if (entry.Metadata.FormatSpecifiers.Count > existing.Metadata.FormatSpecifiers.Count)
                existing.Metadata.FormatSpecifiers = new List<string>(entry.Metadata.FormatSpecifiers);
        }

        // Derive IsTranslated: true if any non-en locale has a non-empty value
        foreach (var entry in grouped.Values)
        {
            var enValue = entry.Values.FirstOrDefault(v => v.Locale == "en")?.Value;
            entry.Metadata.IsTranslated = entry.Values.Any(v =>
                v.Locale != "en" && !string.IsNullOrEmpty(v.Value) && v.Value != enValue);
        }

        return grouped.Values.OrderBy(e => e.Key).ThenBy(e => e.Context).ToList();
    }

    private static string GetGroupingKey(string key, string? context)
    {
        return string.IsNullOrEmpty(context) ? key : $"{key}\0{context}";
    }
}
