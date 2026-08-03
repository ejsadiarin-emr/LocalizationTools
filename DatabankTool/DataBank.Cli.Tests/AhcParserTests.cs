using DataBank.Cli.Parsers;

namespace DataBank.Cli.Tests;

public class AhcParserTests
{
    private static string L10nFilesDir => Path.Combine(
        Directory.GetCurrentDirectory(), "..", "..", "..", "..", "..", "l10n-files");

    [Fact]
    public void Parse_AALM_File_ExtractsAllEntries()
    {
        var filePath = Path.Combine(L10nFilesDir, "AHC", "AALM_dt.cd.ahc");
        var entries = AhcParser.Parse(filePath);

        // 7 keys with values × 4 locales = 28
        // 5 keys with empty values × 4 locales = 20
        // Total: 48 raw entries
        Assert.Equal(48, entries.Count);
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
    public void Parse_AALM_File_ExtractsCorrectKeys()
    {
        var filePath = Path.Combine(L10nFilesDir, "AHC", "AALM_dt.cd.ahc");
        var entries = AhcParser.Parse(filePath);

        var keys = entries.Select(e => e.Key).Distinct().OrderBy(k => k).ToList();

        // Keys with values
        Assert.Contains("Description", keys);
        Assert.Contains("Title", keys);
        Assert.Contains("txtLimits", keys);
        Assert.Contains("txtAlarms", keys);
        Assert.Contains("txtMisc", keys);
        Assert.Contains("txtDiagnostics", keys);
        Assert.Contains("txtEnable", keys);

        // Keys with empty values
        Assert.Contains("chkboxEnable", keys);
        Assert.Contains("txtEnab", keys);
        Assert.Contains("txtOOS", keys);
        Assert.Contains("txtShlv", keys);
        Assert.Contains("txtHelp", keys);

        // Should NOT contain garbage keys
        Assert.DoesNotContain("Value", keys);
        Assert.DoesNotContain("GsLocalizedString", keys);
        Assert.DoesNotContain("EU", keys);
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
    public void Parse_AALM_File_TextKeysHaveCorrectValues()
    {
        var filePath = Path.Combine(L10nFilesDir, "AHC", "AALM_dt.cd.ahc");
        var entries = AhcParser.Parse(filePath);

        var enTxtLimits = entries.First(e => e.Locale == "en" && e.Key == "txtLimits");
        Assert.Equal("txtLimits", enTxtLimits.Value);

        var enTxtAlarms = entries.First(e => e.Locale == "en" && e.Key == "txtAlarms");
        Assert.Equal("txtAlarms", enTxtAlarms.Value);
    }

    [Fact]
    public void Parse_AALM_File_EmptyValueKeysHaveEmptyValues()
    {
        var filePath = Path.Combine(L10nFilesDir, "AHC", "AALM_dt.cd.ahc");
        var entries = AhcParser.Parse(filePath);

        var emptyKeys = new[] { "chkboxEnable", "txtEnab", "txtOOS", "txtShlv", "txtHelp" };
        foreach (var key in emptyKeys)
        {
            var keyEntries = entries.Where(e => e.Key == key).ToList();
            Assert.Equal(4, keyEntries.Count);
            Assert.All(keyEntries, e => Assert.Equal(string.Empty, e.Value));
            Assert.All(keyEntries, e => Assert.False(e.Metadata.IsTranslated));
            Assert.All(keyEntries, e => Assert.Equal("no language value provided", e.Metadata.Comment));
        }
    }

    [Fact]
    public void Parse_AALM_File_GemElementsAreSkipped()
    {
        var filePath = Path.Combine(L10nFilesDir, "AHC", "AALM_dt.cd.ahc");
        var entries = AhcParser.Parse(filePath);

        // Gem elements produce no entries (EU %, NA %, etc.)
        var keys = entries.Select(e => e.Key).Distinct().ToList();
        Assert.DoesNotContain("CD_LABEL_SCALEDVALUE1", keys);
        Assert.DoesNotContain("CD_LABEL_VALUE8", keys);
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
