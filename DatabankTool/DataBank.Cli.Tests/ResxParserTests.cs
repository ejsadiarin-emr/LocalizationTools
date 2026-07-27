using DataBank.Cli.Parsers;

namespace DataBank.Cli.Tests;

public class ResxParserTests
{
    private static string SamplesDir => Path.Combine(
        Directory.GetCurrentDirectory(), "..", "..", "..", "..", "..", "databank-samples");

    [Fact]
    public void Parse_BaseResxFile_ExtractsAllEntries()
    {
        var filePath = Path.Combine(SamplesDir, "resx", "Messages.resx");
        var entries = ResxParser.Parse(filePath);

        Assert.Equal(10, entries.Count);
        Assert.All(entries, e => Assert.Equal("en", e.Locale));
    }

    [Fact]
    public void Parse_FrenchResxFile_DetectsLocale()
    {
        var filePath = Path.Combine(SamplesDir, "resx", "Messages.fr.resx");
        var entries = ResxParser.Parse(filePath);

        Assert.NotEmpty(entries);
        Assert.All(entries, e => Assert.Equal("fr", e.Locale));
    }

    [Fact]
    public void Parse_ChineseResxFile_DetectsComplexLocale()
    {
        var filePath = Path.Combine(SamplesDir, "resx", "Messages.zh-Hans.resx");
        var entries = ResxParser.Parse(filePath);

        Assert.NotEmpty(entries);
        Assert.All(entries, e => Assert.Equal("zh-Hans", e.Locale));
    }

    [Fact]
    public void Parse_EntryWithComment_ExtractsComment()
    {
        var filePath = Path.Combine(SamplesDir, "resx", "Messages.resx");
        var entries = ResxParser.Parse(filePath);

        var welcomeEntry = entries.First(e => e.Key == "WelcomeMessage");
        Assert.Equal("Main greeting shown on application startup", welcomeEntry.Metadata.Comment);
    }

    [Fact]
    public void Parse_EntryWithoutComment_MetadataCommentIsNull()
    {
        var filePath = Path.Combine(SamplesDir, "resx", "Messages.resx");
        var entries = ResxParser.Parse(filePath);

        var errorMessage = entries.First(e => e.Key == "ErrorMessage");
        Assert.Null(errorMessage.Metadata.Comment);
    }

    [Fact]
    public void Parse_EmptyValue_IncludesEntryWithEmptyString()
    {
        var filePath = Path.Combine(SamplesDir, "resx", "Messages.resx");
        var entries = ResxParser.Parse(filePath);

        var emptyEntry = entries.First(e => e.Key == "EmptyValue");
        Assert.Equal(string.Empty, emptyEntry.Value);
    }

    [Fact]
    public void Parse_FileNotFound_ReturnsEmptyList()
    {
        var entries = ResxParser.Parse("nonexistent.resx");
        Assert.Empty(entries);
    }

    [Fact]
    public void Parse_MalformedXml_ReturnsEmptyList()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, "this is not valid xml");
            var entries = ResxParser.Parse(tempFile);
            Assert.Empty(entries);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void DetectLocale_BaseFile_ReturnsEnglish()
    {
        Assert.Equal("en", ResxParser.DetectLocale("Messages.resx"));
    }

    [Fact]
    public void DetectLocale_FrenchFile_ReturnsFr()
    {
        Assert.Equal("fr", ResxParser.DetectLocale("Messages.fr.resx"));
    }

    [Fact]
    public void DetectLocale_ComplexLocale_ReturnsZhHans()
    {
        Assert.Equal("zh-Hans", ResxParser.DetectLocale("Messages.zh-Hans.resx"));
    }

    [Fact]
    public void Parse_XmlSpacePreserve_KeepsWhitespace()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            var xml = """
                <?xml version="1.0" encoding="utf-8"?>
                <root>
                  <data name="Padded" xml:space="preserve">
                    <value>  hello  </value>
                  </data>
                </root>
                """;
            File.WriteAllText(tempFile, xml);
            var entries = ResxParser.Parse(tempFile);

            var entry = entries.First(e => e.Key == "Padded");
            Assert.Equal("  hello  ", entry.Value);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Parse_BinaryDataEntry_SkipsEntry()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            var xml = """
                <?xml version="1.0" encoding="utf-8"?>
                <root>
                  <data name="StringKey">
                    <value>Keep this</value>
                  </data>
                  <data name="BinaryKey" type="System.Drawing.Bitmap, System.Drawing">
                    <value>base64data</value>
                  </data>
                </root>
                """;
            File.WriteAllText(tempFile, xml);
            var entries = ResxParser.Parse(tempFile);

            Assert.Single(entries);
            Assert.Equal("StringKey", entries[0].Key);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }
}
