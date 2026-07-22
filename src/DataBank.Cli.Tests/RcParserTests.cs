using DataBank.Cli.Parsers;

namespace DataBank.Cli.Tests;

public class RcParserTests
{
    private static string SamplesDir => Path.Combine(
        Directory.GetCurrentDirectory(), "..", "..", "..", "..", "..", "samples");

    [Fact]
    public void Parse_SampleRcFile_ExtractsAllEntries()
    {
        var filePath = Path.Combine(SamplesDir, "rc", "app.rc");
        var entries = RcParser.Parse(filePath);

        // 9 en + 9 fr + 5 de = 23
        Assert.Equal(23, entries.Count);
    }

    [Fact]
    public void Parse_WithSymbolMap_ResolvesSymbolicNames()
    {
        var filePath = Path.Combine(SamplesDir, "rc", "app.rc");
        var resourceH = Path.Combine(SamplesDir, "rc", "resource.h");
        var symbolMap = RcParser.ParseResourceH(resourceH);
        var entries = RcParser.Parse(filePath, symbolMap);

        var titleEntry = entries.First(e => e.Locale == "en" && e.Key == "IDS_APP_TITLE");
        Assert.Equal("DeltaV Application", titleEntry.Value);
        Assert.Equal(100, titleEntry.Metadata.RcId);
        Assert.Equal("IDS_APP_TITLE", titleEntry.Metadata.RcDefine);
    }

    [Fact]
    public void Parse_LanguageDirective_DetectsEnglish()
    {
        var filePath = Path.Combine(SamplesDir, "rc", "app.rc");
        var entries = RcParser.Parse(filePath);

        var enEntries = entries.Where(e => e.Locale == "en").ToList();
        Assert.Equal(9, enEntries.Count);
    }

    [Fact]
    public void Parse_LanguageDirective_DetectsFrench()
    {
        var filePath = Path.Combine(SamplesDir, "rc", "app.rc");
        var entries = RcParser.Parse(filePath);

        var frEntries = entries.Where(e => e.Locale == "fr").ToList();
        Assert.Equal(9, frEntries.Count);
    }

    [Fact]
    public void Parse_LanguageDirective_DetectsGerman()
    {
        var filePath = Path.Combine(SamplesDir, "rc", "app.rc");
        var entries = RcParser.Parse(filePath);

        var deEntries = entries.Where(e => e.Locale == "de").ToList();
        Assert.Equal(5, deEntries.Count);
    }

    [Fact]
    public void Parse_FileNotFound_ReturnsEmptyList()
    {
        var entries = RcParser.Parse("nonexistent.rc");
        Assert.Empty(entries);
    }

    [Fact]
    public void UnescapeValue_UnicodePrefix_StripsL()
    {
        Assert.Equal("Hello", RcParser.UnescapeValue("L\"Hello\""));
    }

    [Fact]
    public void UnescapeValue_EscapedQuotes_UnescapesDoubleQuotes()
    {
        Assert.Equal("Say \"hello\"", RcParser.UnescapeValue("Say \"\"hello\"\""));
    }

    [Fact]
    public void UnescapeValue_RegularString_RemovesQuotes()
    {
        Assert.Equal("Hello", RcParser.UnescapeValue("\"Hello\""));
    }

    [Fact]
    public void MapLanguageToLocale_English_ReturnsEn()
    {
        Assert.Equal("en", RcParser.MapLanguageToLocale("LANG_ENGLISH", "SUBLANG_ENGLISH_US"));
    }

    [Fact]
    public void MapLanguageToLocale_French_ReturnsFr()
    {
        Assert.Equal("fr", RcParser.MapLanguageToLocale("LANG_FRENCH", "SUBLANG_FRENCH"));
    }

    [Fact]
    public void MapLanguageToLocale_ChineseSimplified_ReturnsZhHans()
    {
        Assert.Equal("zh-Hans", RcParser.MapLanguageToLocale("LANG_CHINESE", "SUBLANG_CHINESE_SIMPLIFIED"));
    }

    [Fact]
    public void MapLanguageToLocale_Unknown_ReturnsLowercaseLang()
    {
        Assert.Equal("lang_catalan", RcParser.MapLanguageToLocale("LANG_CATALAN", "SUBLANG_DEFAULT"));
    }

    [Fact]
    public void ParseResourceH_FileNotFound_ReturnsEmptyDict()
    {
        var map = RcParser.ParseResourceH("nonexistent.h");
        Assert.Empty(map);
    }
}
