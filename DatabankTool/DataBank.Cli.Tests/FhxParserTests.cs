using DataBank.Cli.Parsers;

namespace DataBank.Cli.Tests;

public class FhxParserTests
{
    private static string L10nFilesDir => Path.Combine(
        Directory.GetCurrentDirectory(), "..", "..", "..", "..", "..", "l10n-files");

    [Fact]
    public void Parse_EnAlarmWords_ExtractsAllEntries()
    {
        var filePath = Path.Combine(L10nFilesDir, "FHX", "EN", "AlarmWords.txt");
        var entries = FhxParser.Parse(filePath);

        Assert.Equal(142, entries.Count);
    }

    [Fact]
    public void Parse_EnAlarmWords_DetectsEnglishLocale()
    {
        var filePath = Path.Combine(L10nFilesDir, "FHX", "EN", "AlarmWords.txt");
        var entries = FhxParser.Parse(filePath);

        Assert.All(entries, e => Assert.Equal("en", e.Locale));
    }

    [Fact]
    public void Parse_EnAlarmWords_PreservesKeyFormat()
    {
        var filePath = Path.Combine(L10nFilesDir, "FHX", "EN", "AlarmWords.txt");
        var entries = FhxParser.Parse(filePath);

        var criticalEntry = entries.First(e => e.Key == "@CRITICAL@");
        Assert.Equal("CRITICAL", criticalEntry.Value);
    }

    [Fact]
    public void Parse_EnAlarmWords_DetectsDoNotTranslate()
    {
        var filePath = Path.Combine(L10nFilesDir, "FHX", "EN", "AlarmWords.txt");
        var entries = FhxParser.Parse(filePath);

        var dntEntries = entries.Where(e => e.Metadata.DoNotTranslate).ToList();
        Assert.NotEmpty(dntEntries);
        Assert.Contains(dntEntries, e => e.Key == "@COMM_ALM@");
    }

    [Fact]
    public void Parse_EnAlarmWords_DetectsFormatSpecifiers()
    {
        var filePath = Path.Combine(L10nFilesDir, "FHX", "EN", "AlarmWords.txt");
        var entries = FhxParser.Parse(filePath);

        var adapterError = entries.First(e => e.Key.Contains("Adapter Error"));
        Assert.True(adapterError.Metadata.IsBehavioral);
        Assert.Contains(adapterError.Metadata.FormatSpecifiers, s => s.Contains('s'));
    }

    [Fact]
    public void Parse_EnAlarmWords_IncludesDoNotTranslateInOutput()
    {
        var filePath = Path.Combine(L10nFilesDir, "FHX", "EN", "AlarmWords.txt");
        var entries = FhxParser.Parse(filePath);

        // DoNotTranslate entries should be included, not excluded
        var dntEntries = entries.Where(e => e.Metadata.DoNotTranslate).ToList();
        Assert.True(dntEntries.Count > 0, "DoNotTranslate entries should be included in output");
    }

    [Fact]
    public void Parse_EnAlarmWords_SetsSourceFormatToFhx()
    {
        var filePath = Path.Combine(L10nFilesDir, "FHX", "EN", "AlarmWords.txt");
        var entries = FhxParser.Parse(filePath);

        Assert.All(entries, e => Assert.Equal("fhx", e.Source.Format));
    }

    [Fact]
    public void Parse_EnAlarmWords_SetsCorrectId()
    {
        var filePath = Path.Combine(L10nFilesDir, "FHX", "EN", "AlarmWords.txt");
        var entries = FhxParser.Parse(filePath);

        var criticalEntry = entries.First(e => e.Key == "@CRITICAL@");
        Assert.Equal("fhx::AlarmWords.txt::@CRITICAL@", criticalEntry.Id);
    }

    [Fact]
    public void Parse_WithLocaleOverride_UsesOverriddenLocale()
    {
        var filePath = Path.Combine(L10nFilesDir, "FHX", "EN", "AlarmWords.txt");
        var entries = FhxParser.Parse(filePath, localeOverride: "fr");

        Assert.All(entries, e => Assert.Equal("fr", e.Locale));
    }

    [Fact]
    public void Parse_EmptyFile_ReturnsEmptyList()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, "");
            var entries = FhxParser.Parse(tempFile);
            Assert.Empty(entries);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Parse_FileNotFound_ReturnsEmptyList()
    {
        var entries = FhxParser.Parse("nonexistent.txt");
        Assert.Empty(entries);
    }

    [Fact]
    public void DetectLocale_EnDirectory_ReturnsEn()
    {
        var path = Path.Combine("some", "path", "FHX", "EN", "AlarmWords.txt");
        Assert.Equal("en", FhxParser.DetectLocale(path, ""));
    }

    [Fact]
    public void DetectLocale_WithOverride_ReturnsOverride()
    {
        var path = Path.Combine("some", "path", "FHX", "EN", "AlarmWords.txt");
        Assert.Equal("de", FhxParser.DetectLocale(path, "", localeOverride: "de"));
    }

    [Fact]
    public void DetectLocale_JpDirectory_ReturnsJa()
    {
        var path = Path.Combine("some", "path", "FHX", "JP", "AlarmWords.txt");
        Assert.Equal("ja", FhxParser.DetectLocale(path, ""));
    }

    [Fact]
    public void DetectLocale_TranslatedDirectory_ReturnsUnknown()
    {
        var path = Path.Combine("some", "path", "FHX", "Translated", "AlarmWords.txt");
        Assert.Equal("unknown", FhxParser.DetectLocale(path, ""));
    }

    [Fact]
    public void DetectLocale_UnknownDir_WithLangTag_ReturnsMapped()
    {
        var path = Path.Combine("some", "path", "FHX", "Translated", "AlarmWords.txt");
        var content = "@Key@\t\"context\"\tSome value // lang=zh-CN";
        Assert.Equal("zh-CN", FhxParser.DetectLocale(path, content));
    }

    [Fact]
    public void NormalizeLangTag_ZhHans_ReturnsZhHans()
    {
        Assert.Equal("zh-CN", FhxParser.NormalizeLangTag("zh-CN"));
        Assert.Equal("zh-CN", FhxParser.NormalizeLangTag("zh-CN"));
    }

    [Fact]
    public void DetectLocaleFromFilePath_FrenchDirectory_ReturnsFr()
    {
        var path = Path.Combine("Code_Locale", "French", "Fhx", "AlarmWords.txt");
        Assert.Equal("fr", FhxParser.DetectLocaleFromFilePath(path));
    }

    [Fact]
    public void DetectLocaleFromFilePath_RussianDirectory_ReturnsRu()
    {
        var path = Path.Combine("Code_Locale", "Russian", "Fhx", "AlarmWords.txt");
        Assert.Equal("ru", FhxParser.DetectLocaleFromFilePath(path));
    }

    [Fact]
    public void DetectLocaleFromFilePath_ChineseDirectory_ReturnsZhHans()
    {
        var path = Path.Combine("Code_Locale", "Chinese", "Fhx", "AlarmWords.txt");
        Assert.Equal("zh-CN", FhxParser.DetectLocaleFromFilePath(path));
    }

    [Fact]
    public void DetectLocaleFromFilePath_JapaneseDirectory_ReturnsJa()
    {
        var path = Path.Combine("Code_Locale", "Japanese", "Fhx", "AlarmWords.txt");
        Assert.Equal("ja", FhxParser.DetectLocaleFromFilePath(path));
    }

    [Fact]
    public void DetectLocaleFromFilePath_LtkDirectory_ReturnsLt()
    {
        var path = Path.Combine("Code_Locale", "LTK", "Fhx", "AlarmWords.txt");
        Assert.Equal("lt", FhxParser.DetectLocaleFromFilePath(path));
    }

    [Fact]
    public void DetectLocaleFromFilePath_NoLocaleDirectory_ReturnsNull()
    {
        var path = Path.Combine("Code", "fhx", "alarmTypes.fhx");
        Assert.Null(FhxParser.DetectLocaleFromFilePath(path));
    }

    [Fact]
    public void DetectLocale_FilePathWithFrenchDirectory_ReturnsFr()
    {
        var path = Path.Combine("Code_Locale", "French", "Fhx", "AlarmWords.txt");
        Assert.Equal("fr", FhxParser.DetectLocale(path, ""));
    }
}
