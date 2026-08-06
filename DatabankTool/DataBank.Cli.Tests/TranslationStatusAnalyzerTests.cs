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
                Id = "test::key1", Key = "key1",
                Values =
                [
                    new LocaleValue { Locale = "en", Value = "English" },
                    new LocaleValue { Locale = "zh-CN", Value = "中文" }
                ],
                Sources = new Dictionary<string, SourceInfo>
                {
                    ["en"] = new SourceInfo { Format = "resx", File = "test.resx", Path = "test.resx" }
                },
                Metadata = new EntryMetadata { IsTranslated = true }
            },
            new()
            {
                Id = "test::key2", Key = "key2",
                Values =
                [
                    new LocaleValue { Locale = "en", Value = "English2" }
                ],
                Sources = new Dictionary<string, SourceInfo>
                {
                    ["en"] = new SourceInfo { Format = "resx", File = "test.resx", Path = "test.resx" }
                },
                Metadata = new EntryMetadata { IsTranslated = true }
            },
            new()
            {
                Id = "test::key3", Key = "key3",
                Values =
                [
                    new LocaleValue { Locale = "en", Value = "English3" },
                    new LocaleValue { Locale = "fr", Value = "" }
                ],
                Sources = new Dictionary<string, SourceInfo>
                {
                    ["en"] = new SourceInfo { Format = "resx", File = "test.resx", Path = "test.resx" }
                },
                Metadata = new EntryMetadata { IsTranslated = true }
            }
        };

        var summary = TranslationStatusAnalyzer.Analyze(entries);

        Assert.NotNull(summary);
        // EN locale is skipped; zh-CN "中文" differs from EN → Translated
        // fr empty → Untranslated; key2 EN-only → not counted
        Assert.Equal(1, summary.Overall.Translated);
        Assert.Equal(1, summary.Overall.Untranslated);

        var key3Entry = entries.First(e => e.Key == "key3");
        Assert.False(string.IsNullOrEmpty(key3Entry.Values.First(v => v.Locale == "fr").Value) == false);
    }

    [Fact]
    public void Analyze_DoNotTranslateEntry_ReceivesDoNotTranslateStatus()
    {
        var entries = new List<LocalizedStringEntry>
        {
            new()
            {
                Id = "test::key1", Key = "key1",
                Values =
                [
                    new LocaleValue { Locale = "en", Value = "English" }
                ],
                Sources = new Dictionary<string, SourceInfo>
                {
                    ["en"] = new SourceInfo { Format = "resx", File = "test.resx", Path = "test.resx" }
                },
                Metadata = new EntryMetadata { DoNotTranslate = true }
            }
        };

        var summary = TranslationStatusAnalyzer.Analyze(entries);

        var enEntry = entries.First(e => e.Key == "key1");
        Assert.Equal(TranslationStatus.DoNotTranslate, enEntry.Metadata.GetDerivedStatus());
        Assert.Equal(1, summary.Overall.DoNotTranslate);
        Assert.Equal(0, summary.Overall.Translated);
    }

    [Fact]
    public void Analyze_EnEntries_AlwaysReceiveTranslatedStatus()
    {
        var entries = new List<LocalizedStringEntry>
        {
            new()
            {
                Id = "test::key1", Key = "key1",
                Values =
                [
                    new LocaleValue { Locale = "en", Value = "English" }
                ],
                Sources = new Dictionary<string, SourceInfo>
                {
                    ["en"] = new SourceInfo { Format = "resx", File = "test.resx", Path = "test.resx" }
                },
                Metadata = new EntryMetadata { IsTranslated = true }
            },
            new()
            {
                Id = "test::key2", Key = "key2",
                Values =
                [
                    new LocaleValue { Locale = "en", Value = "English2" }
                ],
                Sources = new Dictionary<string, SourceInfo>
                {
                    ["en"] = new SourceInfo { Format = "resx", File = "test.resx", Path = "test.resx" }
                },
                Metadata = new EntryMetadata { IsTranslated = true }
            }
        };

        var summary = TranslationStatusAnalyzer.Analyze(entries);

        // EN-only entries: EN locale is skipped, so no translations counted
        Assert.All(entries, e =>
        {
            Assert.True(e.Metadata.IsTranslated);
            Assert.Equal(TranslationStatus.Translated, e.Metadata.GetDerivedStatus());
        });
        Assert.Equal(0, summary.Overall.Translated);
        Assert.Equal(0, summary.Overall.Untranslated);
    }

    [Fact]
    public void Analyze_SummaryCounts_ByLocaleAndOverall()
    {
        var entries = new List<LocalizedStringEntry>
        {
            new()
            {
                Id = "test::key1", Key = "key1",
                Values =
                [
                    new LocaleValue { Locale = "en", Value = "English" },
                    new LocaleValue { Locale = "fr", Value = "French" },
                    new LocaleValue { Locale = "de", Value = "German" }
                ],
                Sources = new Dictionary<string, SourceInfo>
                {
                    ["en"] = new SourceInfo { Format = "resx", File = "test.resx", Path = "test.resx" }
                },
                Metadata = new EntryMetadata { IsTranslated = true }
            },
            new()
            {
                Id = "test::key2", Key = "key2",
                Values =
                [
                    new LocaleValue { Locale = "en", Value = "English2" }
                ],
                Sources = new Dictionary<string, SourceInfo>
                {
                    ["en"] = new SourceInfo { Format = "resx", File = "test.resx", Path = "test.resx" }
                },
                Metadata = new EntryMetadata { IsTranslated = true }
            },
            new()
            {
                Id = "test::key3", Key = "key3",
                Values =
                [
                    new LocaleValue { Locale = "en", Value = "English3" },
                    new LocaleValue { Locale = "de", Value = "" }
                ],
                Sources = new Dictionary<string, SourceInfo>
                {
                    ["en"] = new SourceInfo { Format = "resx", File = "test.resx", Path = "test.resx" }
                },
                Metadata = new EntryMetadata { IsTranslated = true }
            },
            new()
            {
                Id = "test::key4", Key = "key4",
                Values =
                [
                    new LocaleValue { Locale = "en", Value = "English4" },
                    new LocaleValue { Locale = "de", Value = "" }
                ],
                Sources = new Dictionary<string, SourceInfo>
                {
                    ["en"] = new SourceInfo { Format = "resx", File = "test.resx", Path = "test.resx" }
                },
                Metadata = new EntryMetadata { DoNotTranslate = true }
            }
        };

        var summary = TranslationStatusAnalyzer.Analyze(entries);

        // EN locale is skipped; key1 fr+de differ from EN → 2 Translated
        // key3 de is empty → 1 Untranslated; key4 de is DNT → 1 DoNotTranslate
        // key2 EN-only → not counted
        Assert.Equal(2, summary.Overall.Translated);
        Assert.Equal(1, summary.Overall.Untranslated);
        Assert.Equal(1, summary.Overall.DoNotTranslate);

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
                Id = "test::key1", Key = "key1",
                Values =
                [
                    new LocaleValue { Locale = "EN", Value = "English" },
                    new LocaleValue { Locale = "Fr", Value = "French" }
                ],
                Sources = new Dictionary<string, SourceInfo>
                {
                    ["en"] = new SourceInfo { Format = "resx", File = "test.resx", Path = "test.resx" }
                },
                Metadata = new EntryMetadata { IsTranslated = true }
            }
        };

        var summary = TranslationStatusAnalyzer.Analyze(entries);

        var key1Entry = entries.First(e => e.Key == "key1");
        Assert.True(key1Entry.Metadata.IsTranslated);
        Assert.Equal(TranslationStatus.Translated, key1Entry.Metadata.GetDerivedStatus());
    }
}
