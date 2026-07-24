using DataBank.Cli.Parsers;

namespace DataBank.Cli.Tests;

public class AhcParserTests
{
    private static string L10nFilesDir => Path.Combine(
        Directory.GetCurrentDirectory(), "..", "..", "..", "..", "..", "l10n-files");

    [Fact]
    public void Parse_AALM_File_ExtractsNonEmptyEntries()
    {
        var filePath = Path.Combine(L10nFilesDir, "AHC", "AALM_dt.cd.ahc");
        var entries = AhcParser.Parse(filePath);

        // 68 LanguageValues total, 40 non-empty (10 per lang × 4 langs)
        Assert.Equal(40, entries.Count);
    }

    [Fact]
    public void Parse_AALM_File_DetectsAllLanguages()
    {
        var filePath = Path.Combine(L10nFilesDir, "AHC", "AALM_dt.cd.ahc");
        var entries = AhcParser.Parse(filePath);

        var locales = entries.Select(e => e.Locale).Distinct().OrderBy(l => l).ToList();
        Assert.Equal(new[] { "en", "jp", "ru", "zh" }, locales);
    }

    [Fact]
    public void Parse_AALM_File_SetsSourceFormatToAhc()
    {
        var filePath = Path.Combine(L10nFilesDir, "AHC", "AALM_dt.cd.ahc");
        var entries = AhcParser.Parse(filePath);

        Assert.All(entries, e => Assert.Equal("ahc", e.Source.Format));
    }

    [Fact]
    public void Parse_AALM_File_SkipsEmptyEntries()
    {
        var filePath = Path.Combine(L10nFilesDir, "AHC", "AALM_dt.cd.ahc");
        var entries = AhcParser.Parse(filePath);

        Assert.All(entries, e => Assert.False(string.IsNullOrWhiteSpace(e.Value)));
    }

    [Fact]
    public void Parse_AALM_File_ExtractsCorrectValues()
    {
        var filePath = Path.Combine(L10nFilesDir, "AHC", "AALM_dt.cd.ahc");
        var entries = AhcParser.Parse(filePath);

        var zhDescription = entries.First(e => e.Locale == "zh" && e.Key == "Description");
        Assert.Equal("报警模块详细信息", zhDescription.Value);
    }

    [Fact]
    public void Parse_AALM_File_TitleEntryExists()
    {
        var filePath = Path.Combine(L10nFilesDir, "AHC", "AALM_dt.cd.ahc");
        var entries = AhcParser.Parse(filePath);

        var enTitle = entries.First(e => e.Locale == "en" && e.Key == "Title");
        Assert.Equal("Alarm module detail display", enTitle.Value);
    }

    [Fact]
    public void Parse_EmptyFile_ReturnsEmptyList()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, "");
            var entries = AhcParser.Parse(tempFile);
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
        var entries = AhcParser.Parse("nonexistent.ahc");
        Assert.Empty(entries);
    }

    [Fact]
    public void Parse_MalformedXml_ReturnsEmptyList()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, "this is not valid xml");
            var entries = AhcParser.Parse(tempFile);
            Assert.Empty(entries);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }
}
