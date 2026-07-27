using DataBank.Cli.Models;

namespace DataBank.Cli.Tests;

public class TranslationStatusAnalyzerTests
{
    [Fact]
    public void Analyze_MixedEntries_AssignsCorrectStatuses()
    {
        var entries = new List<LocalizedStringEntry>
        {
            new()
            {
                Id = "test::en::key1", Key = "key1", Value = "English", Locale = "en",
                Source = new SourceInfo { Format = "resx", File = "test.resx", Path = "test.resx" }
            },
            new()
            {
                Id = "test::en::key2", Key = "key2", Value = "English2", Locale = "en",
                Source = new SourceInfo { Format = "resx", File = "test.resx", Path = "test.resx" }
            },
            new()
            {
                Id = "test::fr::key1", Key = "key1", Value = "French", Locale = "fr",
                Source = new SourceInfo { Format = "resx", File = "test.fr.resx", Path = "test.fr.resx" }
            },
            new()
            {
                Id = "test::fr::key3", Key = "key3", Value = "French3", Locale = "fr",
                Source = new SourceInfo { Format = "resx", File = "test.fr.resx", Path = "test.fr.resx" }
            }
        };

        var summary = TranslationStatusAnalyzer.Analyze(entries);

        Assert.NotNull(summary);
        Assert.Equal(3, summary.Overall.Translated);
        Assert.Equal(1, summary.Overall.NeedsReview);
        Assert.Equal(1, summary.Overall.Untranslated);

        var enEntry = entries.First(e => e.Locale == "en" && e.Key == "key1");
        Assert.True(enEntry.Metadata.IsTranslated);
        Assert.Equal(TranslationStatus.Translated, enEntry.Metadata.TranslationStatus);

        var frEntry = entries.First(e => e.Locale == "fr" && e.Key == "key1");
        Assert.True(frEntry.Metadata.IsTranslated);
        Assert.Equal(TranslationStatus.Translated, frEntry.Metadata.TranslationStatus);

        var frOrphan = entries.First(e => e.Locale == "fr" && e.Key == "key3");
        Assert.False(frOrphan.Metadata.IsTranslated);
        Assert.Equal(TranslationStatus.NeedsReview, frOrphan.Metadata.TranslationStatus);
    }

    [Fact]
    public void Analyze_DoNotTranslateEntry_ReceivesDoNotTranslateStatus()
    {
        var entries = new List<LocalizedStringEntry>
        {
            new()
            {
                Id = "test::en::key1", Key = "key1", Value = "English", Locale = "en",
                Source = new SourceInfo { Format = "resx", File = "test.resx", Path = "test.resx" }
            },
            new()
            {
                Id = "test::fr::key1", Key = "key1", Value = "French", Locale = "fr",
                Source = new SourceInfo { Format = "resx", File = "test.fr.resx", Path = "test.fr.resx" },
                Metadata = new EntryMetadata { DoNotTranslate = true }
            }
        };

        var summary = TranslationStatusAnalyzer.Analyze(entries);

        var frEntry = entries.First(e => e.Locale == "fr" && e.Key == "key1");
        Assert.False(frEntry.Metadata.IsTranslated);
        Assert.Equal(TranslationStatus.DoNotTranslate, frEntry.Metadata.TranslationStatus);
        Assert.Equal(1, summary.Overall.DoNotTranslate);
        Assert.Equal(1, summary.Overall.Translated);
    }

    [Fact]
    public void Analyze_EnEntries_AlwaysReceiveTranslatedStatus()
    {
        var entries = new List<LocalizedStringEntry>
        {
            new()
            {
                Id = "test::en::key1", Key = "key1", Value = "English", Locale = "en",
                Source = new SourceInfo { Format = "resx", File = "test.resx", Path = "test.resx" }
            },
            new()
            {
                Id = "test::en::key2", Key = "key2", Value = "English2", Locale = "en",
                Source = new SourceInfo { Format = "resx", File = "test.resx", Path = "test.resx" }
            }
        };

        var summary = TranslationStatusAnalyzer.Analyze(entries);

        Assert.All(entries, e =>
        {
            Assert.True(e.Metadata.IsTranslated);
            Assert.Equal(TranslationStatus.Translated, e.Metadata.TranslationStatus);
        });
        Assert.Equal(2, summary.Overall.Translated);
        Assert.Equal(0, summary.Overall.Untranslated);
    }

    [Fact]
    public void Analyze_SummaryCounts_ByLocaleAndOverall()
    {
        var entries = new List<LocalizedStringEntry>
        {
            new()
            {
                Id = "test::en::key1", Key = "key1", Value = "English", Locale = "en",
                Source = new SourceInfo { Format = "resx", File = "test.resx", Path = "test.resx" }
            },
            new()
            {
                Id = "test::en::key2", Key = "key2", Value = "English2", Locale = "en",
                Source = new SourceInfo { Format = "resx", File = "test.resx", Path = "test.resx" }
            },
            new()
            {
                Id = "test::fr::key1", Key = "key1", Value = "French", Locale = "fr",
                Source = new SourceInfo { Format = "resx", File = "test.fr.resx", Path = "test.fr.resx" }
            },
            new()
            {
                Id = "test::de::key1", Key = "key1", Value = "German", Locale = "de",
                Source = new SourceInfo { Format = "resx", File = "test.de.resx", Path = "test.de.resx" }
            },
            new()
            {
                Id = "test::de::key3", Key = "key3", Value = "German3", Locale = "de",
                Source = new SourceInfo { Format = "resx", File = "test.de.resx", Path = "test.de.resx" },
                Metadata = new EntryMetadata { DoNotTranslate = true }
            }
        };

        var summary = TranslationStatusAnalyzer.Analyze(entries);

        Assert.Equal(4, summary.Overall.Translated);
        Assert.Equal(2, summary.Overall.Untranslated);
        Assert.Equal(1, summary.Overall.DoNotTranslate);
        Assert.Equal(0, summary.Overall.NeedsReview);

        var frLocale = summary.ByLocale.First(l => l.Locale == "fr");
        Assert.Equal(1, frLocale.Translated);
        Assert.Equal(1, frLocale.Untranslated);

        var deLocale = summary.ByLocale.First(l => l.Locale == "de");
        Assert.Equal(1, deLocale.Translated);
        Assert.Equal(1, deLocale.DoNotTranslate);
        Assert.Equal(1, deLocale.Untranslated);
    }

    [Fact]
    public void Analyze_EmptyEntries_ReturnsEmptySummary()
    {
        var entries = new List<LocalizedStringEntry>();

        var summary = TranslationStatusAnalyzer.Analyze(entries);

        Assert.NotNull(summary);
        Assert.Equal(0, summary.Overall.Translated);
        Assert.Equal(0, summary.Overall.Untranslated);
        Assert.Empty(summary.ByLocale);
    }

    [Fact]
    public void Analyze_CaseInsensitiveLocale_HandledCorrectly()
    {
        var entries = new List<LocalizedStringEntry>
        {
            new()
            {
                Id = "test::EN::key1", Key = "key1", Value = "English", Locale = "EN",
                Source = new SourceInfo { Format = "resx", File = "test.resx", Path = "test.resx" }
            },
            new()
            {
                Id = "test::Fr::key1", Key = "key1", Value = "French", Locale = "Fr",
                Source = new SourceInfo { Format = "resx", File = "test.fr.resx", Path = "test.fr.resx" }
            }
        };

        var summary = TranslationStatusAnalyzer.Analyze(entries);

        var enEntry = entries.First(e => e.Key == "key1" && e.Locale == "EN");
        Assert.True(enEntry.Metadata.IsTranslated);
        Assert.Equal(TranslationStatus.Translated, enEntry.Metadata.TranslationStatus);

        var frEntry = entries.First(e => e.Key == "key1" && e.Locale == "Fr");
        Assert.True(frEntry.Metadata.IsTranslated);
        Assert.Equal(TranslationStatus.Translated, frEntry.Metadata.TranslationStatus);
    }
}
