using System.Text;
using DataBank.Cli.Helpers;
using DataBank.Cli.Models;
using DataBank.Cli.Replacers;

namespace DataBank.Cli.Tests;

public class FileWriterTests : IDisposable
{
    private readonly string _testDir;

    public FileWriterTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"databank-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir))
            Directory.Delete(_testDir, true);
    }

    [Fact]
    public void EditEntry_RcFile_PreservesEncoding()
    {
        // Create a UTF-16LE RC file
        var rcContent = "LANGUAGE LANG_FRENCH, SUBLANG_FRENCH\r\nSTRINGTABLE\r\nBEGIN\r\n    IDS_WELCOME \"Bienvenue\"\r\nEND";
        var filePath = Path.Combine(_testDir, "test.rc");
        File.WriteAllText(filePath, rcContent, System.Text.Encoding.Unicode);

        var entry = new RawLocalizedEntry
        {
            Key = "IDS_WELCOME",
            Locale = "fr",
            Value = "Bienvenue",
            Source = new SourceInfo
            {
                Format = "rc",
                File = filePath,
                Path = filePath,
                Line = 4
            }
        };

        var writer = new FileWriter();
        var result = writer.EditEntry(entry, "Salut");

        Assert.True(result.Success);
        Assert.Equal("Bienvenue", result.OldValue);
        Assert.Equal("Salut", result.NewValue);
        Assert.Equal(4, result.Line);

        // Verify file is still UTF-16LE
        var bytes = File.ReadAllBytes(filePath);
        Assert.Equal(0xFF, bytes[0]);
        Assert.Equal(0xFE, bytes[1]);

        // Verify content
        var newContent = File.ReadAllText(filePath, System.Text.Encoding.Unicode);
        Assert.Contains("IDS_WELCOME \"Salut\"", newContent);
    }

    [Fact]
    public void EditEntry_FhxFile_PreservesEncoding()
    {
        // Create a UTF-16LE FHX file
        var fhxContent = "@Key@\t\"context\"\tOld Value";
        var filePath = Path.Combine(_testDir, "test.txt");
        File.WriteAllText(filePath, fhxContent, System.Text.Encoding.Unicode);

        var entry = new RawLocalizedEntry
        {
            Key = "@Key@",
            Locale = "en",
            Value = "Old Value",
            Source = new SourceInfo
            {
                Format = "fhx",
                File = filePath,
                Path = filePath,
                Line = 1
            }
        };

        var writer = new FileWriter();
        var result = writer.EditEntry(entry, "New Value");

        Assert.True(result.Success);

        // Verify file is still UTF-16LE
        var bytes = File.ReadAllBytes(filePath);
        Assert.Equal(0xFF, bytes[0]);
        Assert.Equal(0xFE, bytes[1]);

        // Verify content
        var newContent = File.ReadAllText(filePath, System.Text.Encoding.Unicode);
        Assert.Contains("@Key@\t\"context\"\tNew Value", newContent);
    }

    [Fact]
    public void EditEntry_Utf8File_PreservesEncoding()
    {
        // Create a UTF-8 file
        var jsonContent = "  \"key\": \"Old Value\"";
        var filePath = Path.Combine(_testDir, "test.json");
        File.WriteAllText(filePath, jsonContent, System.Text.Encoding.UTF8);

        var entry = new RawLocalizedEntry
        {
            Key = "key",
            Locale = "en",
            Value = "Old Value",
            Source = new SourceInfo
            {
                Format = "json",
                File = filePath,
                Path = filePath,
                Line = 1
            }
        };

        var writer = new FileWriter();
        var result = writer.EditEntry(entry, "New Value");

        Assert.True(result.Success);

        // Verify content
        var newContent = File.ReadAllText(filePath, System.Text.Encoding.UTF8);
        Assert.Contains("\"key\": \"New Value\"", newContent);
    }

    [Fact]
    public void EditEntry_OldValueMismatch_ReturnsError()
    {
        var rcContent = "STRINGTABLE\r\nBEGIN\r\n    IDS_WELCOME \"Bienvenue\"\r\nEND";
        var filePath = Path.Combine(_testDir, "test.rc");
        File.WriteAllText(filePath, rcContent, System.Text.Encoding.Unicode);

        var entry = new RawLocalizedEntry
        {
            Key = "IDS_WELCOME",
            Locale = "fr",
            Value = "WrongValue",
            Source = new SourceInfo
            {
                Format = "rc",
                File = filePath,
                Path = filePath,
                Line = 3
            }
        };

        var writer = new FileWriter();
        var result = writer.EditEntry(entry, "Salut");

        Assert.False(result.Success);
        Assert.Contains("Old value not found", result.ErrorMessage);
    }

    [Fact]
    public void EditEntry_LineOutOfBounds_ReturnsError()
    {
        var rcContent = "STRINGTABLE\r\nBEGIN\r\n    IDS_WELCOME \"Bienvenue\"\r\nEND";
        var filePath = Path.Combine(_testDir, "test.rc");
        File.WriteAllText(filePath, rcContent, System.Text.Encoding.Unicode);

        var entry = new RawLocalizedEntry
        {
            Key = "IDS_WELCOME",
            Locale = "fr",
            Value = "Bienvenue",
            Source = new SourceInfo
            {
                Format = "rc",
                File = filePath,
                Path = filePath,
                Line = 100
            }
        };

        var writer = new FileWriter();
        var result = writer.EditEntry(entry, "Salut");

        Assert.False(result.Success);
        Assert.Contains("does not exist", result.ErrorMessage);
    }

    [Fact]
    public void EditEntry_FileNotFound_ReturnsError()
    {
        var entry = new RawLocalizedEntry
        {
            Key = "IDS_WELCOME",
            Locale = "fr",
            Value = "Bienvenue",
            Source = new SourceInfo
            {
                Format = "rc",
                File = "nonexistent.rc",
                Path = "nonexistent.rc",
                Line = 1
            }
        };

        var writer = new FileWriter();
        var result = writer.EditEntry(entry, "Salut");

        Assert.False(result.Success);
        Assert.Contains("File not found", result.ErrorMessage);
    }

    [Fact]
    public void EditEntry_UnknownFormat_ReturnsError()
    {
        var filePath = Path.Combine(_testDir, "test.xyz");
        File.WriteAllText(filePath, "content");

        var entry = new RawLocalizedEntry
        {
            Key = "KEY",
            Locale = "en",
            Value = "content",
            Source = new SourceInfo
            {
                Format = "xyz",
                File = filePath,
                Path = filePath,
                Line = 1
            }
        };

        var writer = new FileWriter();
        var result = writer.EditEntry(entry, "new content");

        Assert.False(result.Success);
        Assert.Contains("No format replacer", result.ErrorMessage);
    }

    [Fact]
    public void EditEntry_ResxFile_WritesToValueElement()
    {
        // Real multi-line RESX format: <value> is on a separate line from <data>
        var resxContent = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<root>\r\n  <data name=\"Username\" xml:space=\"preserve\">\r\n    <value>User Name</value>\r\n  </data>\r\n</root>";
        var filePath = Path.Combine(_testDir, "Strings.resx");
        File.WriteAllText(filePath, resxContent, new System.Text.UTF8Encoding(false));

        var entry = new RawLocalizedEntry
        {
            Key = "Username",
            Locale = "en",
            Value = "User Name",
            Source = new SourceInfo
            {
                Format = "resx",
                File = filePath,
                Path = filePath,
                Line = 4  // line of <value> element
            }
        };

        var writer = new FileWriter();
        var result = writer.EditEntry(entry, "Nom d\u0027utilisateur");

        Assert.True(result.Success);
        Assert.Contains("<value>Nom d&#39;utilisateur</value>", File.ReadAllText(filePath));
    }

    [Fact]
    public void EditEntry_AhcFile_WritesToContentElement()
    {
        // Real multi-line AHC format: <Content> is on a separate line from <LanguageValue>
        var ahcContent = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<ContextualDisplay Name=\"test\">\r\n  <ContainedElements>\r\n    <Text Name=\"key1\">\r\n      <LanguageValues>\r\n        <LanguageValue Name=\"en\">\r\n          <Content>Alarm module detail display</Content>\r\n        </LanguageValue>\r\n      </LanguageValues>\r\n    </Text>\r\n  </ContainedElements>\r\n</ContextualDisplay>";
        var filePath = Path.Combine(_testDir, "test.ahc");
        File.WriteAllText(filePath, ahcContent, new System.Text.UTF8Encoding(false));

        var entry = new RawLocalizedEntry
        {
            Key = "key1",
            Locale = "en",
            Value = "Alarm module detail display",
            Source = new SourceInfo
            {
                Format = "ahc",
                File = filePath,
                Path = filePath,
                Line = 7  // line of <Content> element
            }
        };

        var writer = new FileWriter();
        var result = writer.EditEntry(entry, "Affichage d\u00e9taill\u00e9 du module d\u00e9alarme");

        Assert.True(result.Success, result.ErrorMessage);

        var afterContent = File.ReadAllText(filePath);
        Assert.Contains("<Content>Affichage d&#233;taill&#233; du module d&#233;alarme</Content>", afterContent);
    }

    [Fact]
    public void EditEntry_Utf8NoBom_PreservesNoBom()
    {
        // Create a UTF-8 file WITHOUT BOM
        var jsonContent = "  \"key\": \"Old Value\"";
        var filePath = Path.Combine(_testDir, "nobom.json");
        File.WriteAllText(filePath, jsonContent, new System.Text.UTF8Encoding(false));

        var entry = new RawLocalizedEntry
        {
            Key = "key",
            Locale = "en",
            Value = "Old Value",
            Source = new SourceInfo
            {
                Format = "json",
                File = filePath,
                Path = filePath,
                Line = 1
            }
        };

        var writer = new FileWriter();
        var result = writer.EditEntry(entry, "New Value");

        Assert.True(result.Success);

        // Verify file still has NO BOM
        var bytes = File.ReadAllBytes(filePath);
        Assert.NotEqual(0xEF, bytes[0]);
        Assert.NotEqual(0xBB, bytes[1]);
        Assert.NotEqual(0xBF, bytes[2]);

        // Verify content
        var newContent = File.ReadAllText(filePath, new System.Text.UTF8Encoding(false));
        Assert.Contains("\"key\": \"New Value\"", newContent);
    }

    [Fact]
    public void EditEntry_Utf8WithBom_PreservesBom()
    {
        // Create a UTF-8 file WITH BOM
        var jsonContent = "  \"key\": \"Old Value\"";
        var filePath = Path.Combine(_testDir, "withbom.json");
        File.WriteAllText(filePath, jsonContent, new System.Text.UTF8Encoding(true));

        var entry = new RawLocalizedEntry
        {
            Key = "key",
            Locale = "en",
            Value = "Old Value",
            Source = new SourceInfo
            {
                Format = "json",
                File = filePath,
                Path = filePath,
                Line = 1
            }
        };

        var writer = new FileWriter();
        var result = writer.EditEntry(entry, "New Value");

        Assert.True(result.Success);

        // Verify file still HAS BOM
        var bytes = File.ReadAllBytes(filePath);
        Assert.Equal(0xEF, bytes[0]);
        Assert.Equal(0xBB, bytes[1]);
        Assert.Equal(0xBF, bytes[2]);
    }

    [Fact]
    public void EditEntry_LfLineEnding_PreservesLf()
    {
        // Create a file with LF line endings
        var fhxContent = "@Key@\t\"context\"\tOld Value\n@Key2@\t\"context2\"\tOld Value2";
        var filePath = Path.Combine(_testDir, "lf.txt");
        File.WriteAllText(filePath, fhxContent, new System.Text.UTF8Encoding(false));

        var entry = new RawLocalizedEntry
        {
            Key = "@Key@",
            Locale = "en",
            Value = "Old Value",
            Source = new SourceInfo
            {
                Format = "fhx",
                File = filePath,
                Path = filePath,
                Line = 1
            }
        };

        var writer = new FileWriter();
        var result = writer.EditEntry(entry, "New Value");

        Assert.True(result.Success);

        // Verify LF line endings preserved
        var rawBytes = File.ReadAllText(filePath);
        Assert.DoesNotContain("\r\n", rawBytes);
        Assert.Contains("@Key@\t\"context\"\tNew Value\n@Key2@\t\"context2\"\tOld Value2", rawBytes);
    }

    [Fact]
    public void EditEntry_AnsiCodePage_PreservesEncoding()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        // Create an ANSI file with a #pragma code_page(1252) directive
        var rcContent = "#pragma code_page(1252)\r\nSTRINGTABLE\r\nBEGIN\r\nIDS_WELCOME \"Welcome\"\r\nEND";
        var filePath = Path.Combine(_testDir, "ansi.rc");
        File.WriteAllText(filePath, rcContent, Encoding.GetEncoding(1252));

        var entry = new RawLocalizedEntry
        {
            Key = "IDS_WELCOME",
            Locale = "en",
            Value = "Welcome",
            Source = new SourceInfo
            {
                Format = "rc",
                File = filePath,
                Path = filePath,
                Line = 4
            }
        };

        var writer = new FileWriter();
        var result = writer.EditEntry(entry, "Bienvenue");

        Assert.True(result.Success, result.ErrorMessage);

        // Verify content was written
        var content = File.ReadAllText(filePath, Encoding.GetEncoding(1252));
        Assert.Contains("IDS_WELCOME \"Bienvenue\"", content);

        // Verify encoding preserved (no BOM introduced, reads correctly as 1252)
        var bytes = File.ReadAllBytes(filePath);
        Assert.NotEqual(0xEF, bytes[0]);
    }
}
