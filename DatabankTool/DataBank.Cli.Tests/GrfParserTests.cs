using DataBank.Cli.Parsers;

namespace DataBank.Cli.Tests;

public class GrfParserTests
{
    [Fact]
    public void Parse_GrfFile_ReturnsSingleEntry()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"GrfTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var grfFile = Path.Combine(tempDir, "AlarmFilter.grf");
        try
        {
            File.WriteAllBytes(grfFile, [0x01, 0x02, 0x03]);
            var entries = GrfParser.Parse(grfFile);

            Assert.Single(entries);
            Assert.Equal("grf", entries[0].Source.Format);
            Assert.Equal("AlarmFilter", entries[0].Key);
            Assert.Equal("GRF file: AlarmFilter.grf", entries[0].Value);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void Parse_GrfFile_WithRootDir_SetsRelativePath()
    {
        var rootDir = Path.Combine(Path.GetTempPath(), $"GrfTest_{Guid.NewGuid():N}");
        var grfDir = Path.Combine(rootDir, "GRF", "EN");
        Directory.CreateDirectory(grfDir);
        var grfFile = Path.Combine(grfDir, "AlarmFilter.grf");
        try
        {
            File.WriteAllBytes(grfFile, [0x01]);
            var entries = GrfParser.Parse(grfFile, rootDir);

            Assert.Single(entries);
            Assert.Equal("GRF\\EN\\AlarmFilter.grf", entries[0].Source.File);
        }
        finally
        {
            Directory.Delete(rootDir, true);
        }
    }

    [Fact]
    public void DetectLocale_FromFilePath_EN()
    {
        var locale = GrfParser.DetectLocale(@"C:\l10n-files\GRF\EN\AlarmFilter.grf");
        Assert.Equal("en", locale);
    }

    [Fact]
    public void DetectLocale_FromFilePath_Chinese()
    {
        var locale = GrfParser.DetectLocale(@"C:\l10n-files\GRF\Translated\Chinese\AlarmFilter.grf");
        Assert.Equal("zh-CN", locale);
    }

    [Fact]
    public void DetectLocale_FromFilePath_Unknown()
    {
        var locale = GrfParser.DetectLocale(@"C:\l10n-files\GRF\Translated\AlarmFilter.grf");
        Assert.Equal("unknown", locale);
    }

    [Fact]
    public void DetectLocale_FromFileName_French()
    {
        var locale = GrfParser.DetectLocale(@"C:\some\path\AlarmFilter.fr.grf");
        Assert.Equal("fr", locale);
    }

    [Fact]
    public void DetectLocale_FromFileName_ChineseSimplified()
    {
        var locale = GrfParser.DetectLocale(@"C:\some\path\AlarmFilter.zh-Hans.grf");
        Assert.Equal("zh-CN", locale);
    }

    [Fact]
    public void DetectLocale_FromFileName_NoLocale()
    {
        var locale = GrfParser.DetectLocale(@"C:\some\path\AlarmFilter.grf");
        Assert.Equal("unknown", locale);
    }

    [Fact]
    public void DetectLocale_FilePathTakesPrecedence()
    {
        var locale = GrfParser.DetectLocale(@"C:\GRF\EN\AlarmFilter.fr.grf");
        Assert.Equal("en", locale);
    }
}
