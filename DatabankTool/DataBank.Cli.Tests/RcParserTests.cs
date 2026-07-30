using DataBank.Cli.Parsers;

namespace DataBank.Cli.Tests;

public class RcParserTests
{
    private static string SamplesDir => Path.Combine(
        Directory.GetCurrentDirectory(), "..", "..", "..", "TestData");

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
        Assert.Equal("zh-CN", RcParser.MapLanguageToLocale("LANG_CHINESE", "SUBLANG_CHINESE_SIMPLIFIED"));
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

    [Fact]
    public void Parse_DialogBlock_ExtractsCaptionAndControls()
    {
        var rc = """
            LANGUAGE LANG_ENGLISH, SUBLANG_ENGLISH_US

            IDD_ABOUTBOX DIALOG 0, 0, 295, 55
            STYLE DS_SETFONT | DS_MODALFRAME | WS_POPUP | WS_CAPTION | WS_SYSMENU
            CAPTION "About DeltaV"
            FONT 8, "MS Sans Serif"
            BEGIN
                LTEXT           "Version 5.2",IDC_STATIC,40,14,159,8
                PUSHBUTTON      "OK",IDOK,238,34,32,14
            END
            """;
        var entries = ParseRcString(rc);

        Assert.Equal(3, entries.Count);

        var caption = entries.First(e => e.Key.StartsWith("CAPTION"));
        Assert.Equal("About DeltaV", caption.Value);
        Assert.Equal("en", caption.Locale);

        var ltext = entries.First(e => e.Key.StartsWith("LTEXT"));
        Assert.Equal("Version 5.2", ltext.Value);

        var pushbtn = entries.First(e => e.Key.StartsWith("PUSHBUTTON"));
        Assert.Equal("OK", pushbtn.Value);
    }

    [Fact]
    public void Parse_DialogExBlock_ExtractsEntries()
    {
        var rc = """
            LANGUAGE LANG_ENGLISH, SUBLANG_ENGLISH_US

            IDD_MAIN DIALOGEX 0, 0, 235, 298
            STYLE DS_SETFONT | DS_MODALFRAME
            CAPTION "Main Dialog"
            FONT 8, "MS Sans Serif"
            BEGIN
                DEFPUSHBUTTON   "Next ->",IDC_NEXT,65,277,50,14
                GROUPBOX        "Options",IDC_STATIC,56,165,168,79
                CTEXT           "Centered Text",IDC_STATIC,40,14,159,8
            END
            """;
        var entries = ParseRcString(rc);

        Assert.Equal(4, entries.Count);
        Assert.Contains(entries, e => e.Key.StartsWith("CAPTION"));
        Assert.Contains(entries, e => e.Key.StartsWith("DEFPUSHBUTTON"));
        Assert.Contains(entries, e => e.Key.StartsWith("GROUPBOX"));
        Assert.Contains(entries, e => e.Key.StartsWith("CTEXT"));
    }

    [Fact]
    public void Parse_DialogControl_EmptyString_Skipped()
    {
        var rc = """
            LANGUAGE LANG_ENGLISH, SUBLANG_ENGLISH_US

            IDD_TEST DIALOGEX 0, 0, 200, 100
            BEGIN
                LTEXT           "",IDC_STATIC,10,10,100,14
                PUSHBUTTON      "OK",IDOK,10,30,50,14
            END
            """;
        var entries = ParseRcString(rc);

        // Empty LTEXT should be skipped
        Assert.Single(entries);
        Assert.Equal("OK", entries[0].Value);
    }

    [Fact]
    public void Parse_DialogControl_WithFormatSpecifier_SetsBehavioral()
    {
        var rc = """
            LANGUAGE LANG_ENGLISH, SUBLANG_ENGLISH_US

            IDD_TEST DIALOGEX 0, 0, 200, 100
            BEGIN
                LTEXT           "Pressure: %d psi",IDC_STATIC,10,10,100,14
                PUSHBUTTON      "%s",IDOK,10,30,50,14
            END
            """;
        var entries = ParseRcString(rc);

        Assert.Equal(2, entries.Count);
        Assert.All(entries, e => Assert.NotEmpty(e.Metadata.FormatSpecifiers));
        Assert.Single(entries[0].Metadata.FormatSpecifiers); // %d in LTEXT
        Assert.Single(entries[1].Metadata.FormatSpecifiers); // %s in PUSHBUTTON
    }

    [Fact]
    public void Parse_DialogControl_NoFormatSpecifier_NotBehavioral()
    {
        var rc = """
            LANGUAGE LANG_ENGLISH, SUBLANG_ENGLISH_US

            IDD_TEST DIALOGEX 0, 0, 200, 100
            BEGIN
                LTEXT           "Hello World",IDC_STATIC,10,10,100,14
            END
            """;
        var entries = ParseRcString(rc);

        Assert.Single(entries);
        Assert.Empty(entries[0].Metadata.FormatSpecifiers);
    }

    [Fact]
    public void Parse_DialogControl_LiteralDoublePercent_NotFormatSpecifier()
    {
        var rc = """
            LANGUAGE LANG_ENGLISH, SUBLANG_ENGLISH_US

            IDD_TEST DIALOGEX 0, 0, 200, 100
            BEGIN
                LTEXT           "100%% complete",IDC_STATIC,10,10,100,14
            END
            """;
        var entries = ParseRcString(rc);

        Assert.Single(entries);
        Assert.Empty(entries[0].Metadata.FormatSpecifiers);
    }

    [Fact]
    public void Parse_DialogControl_WithSymbolMap_ResolvesIds()
    {
        var rc = """
            LANGUAGE LANG_ENGLISH, SUBLANG_ENGLISH_US

            IDD_TEST DIALOGEX 0, 0, 200, 100
            BEGIN
                PUSHBUTTON      "Submit",IDC_SUBMIT_BTN,10,30,50,14
            END
            """;
        var symbolMap = new Dictionary<int, string> { { 500, "IDC_SUBMIT_BTN" } };
        var entries = ParseRcString(rc, symbolMap);

        Assert.Single(entries);
        Assert.Contains("IDC_SUBMIT_BTN", entries[0].Key);
    }

    [Fact]
    public void Parse_Dialog_TwoBlocks_BothParsed()
    {
        var rc = """
            LANGUAGE LANG_ENGLISH, SUBLANG_ENGLISH_US

            IDD_FIRST DIALOGEX 0, 0, 200, 100
            BEGIN
                LTEXT           "First Dialog",IDC_STATIC,10,10,100,14
            END

            IDD_SECOND DIALOGEX 0, 0, 200, 100
            BEGIN
                LTEXT           "Second Dialog",IDC_STATIC,10,10,100,14
            END
            """;
        var entries = ParseRcString(rc);

        Assert.Equal(2, entries.Count);
        Assert.Contains(entries, e => e.Value == "First Dialog");
        Assert.Contains(entries, e => e.Value == "Second Dialog");
    }

    [Fact]
    public void Parse_DesignInfoBlock_Skipped()
    {
        var rc = """
            LANGUAGE LANG_ENGLISH, SUBLANG_ENGLISH_US

            #ifdef APSTUDIO_INVOKED
            BEGIN
                IDD_TEST, DIALOG
            END
            #endif

            IDD_TEST DIALOGEX 0, 0, 200, 100
            BEGIN
                LTEXT           "Real Content",IDC_STATIC,10,10,100,14
            END
            """;
        var entries = ParseRcString(rc);

        Assert.Single(entries);
        Assert.Equal("Real Content", entries[0].Value);
    }

    [Fact]
    public void Parse_DialogWithLanguageDirective_LocaleScoping()
    {
        var rc = """
            LANGUAGE LANG_ENGLISH, SUBLANG_ENGLISH_US

            IDD_TEST DIALOGEX 0, 0, 200, 100
            BEGIN
                LTEXT           "English",IDC_STATIC,10,10,100,14
            END

            LANGUAGE LANG_FRENCH, SUBLANG_FRENCH

            IDD_TEST DIALOGEX 0, 0, 200, 100
            BEGIN
                LTEXT           "French",IDC_STATIC,10,10,100,14
            END
            """;
        var entries = ParseRcString(rc);

        Assert.Equal(2, entries.Count);
        var enEntry = entries.First(e => e.Locale == "en");
        Assert.Equal("English", enEntry.Value);
        var frEntry = entries.First(e => e.Locale == "fr");
        Assert.Equal("French", frEntry.Value);
    }

    [Fact]
    public void Parse_DialogControl_ClassNameSkipped_OnlyTextCaptured()
    {
        var rc = """
            LANGUAGE LANG_ENGLISH, SUBLANG_ENGLISH_US

            IDD_TEST DIALOGEX 0, 0, 200, 100
            BEGIN
                CONTROL         "My Checkbox",IDC_CHECK,"Button",BS_AUTOCHECKBOX,10,10,100,14
            END
            """;
        var entries = ParseRcString(rc);

        Assert.Single(entries);
        Assert.Equal("My Checkbox", entries[0].Value);
        Assert.StartsWith("CONTROL::", entries[0].Key);
    }

    [Fact]
    public void Parse_ControlWithEmptyText_Skipped()
    {
        var rc = """
            LANGUAGE LANG_ENGLISH, SUBLANG_ENGLISH_US

            IDD_TEST DIALOGEX 0, 0, 200, 100
            BEGIN
                CONTROL         "",IDC_STATIC,"Static",SS_BLACKFRAME,10,10,100,14
            END
            """;
        var entries = ParseRcString(rc);

        Assert.Empty(entries);
    }

    [Fact]
    public void DetectFormatSpecifiers_MultipleSpecifiers_AllDetected()
    {
        var metadata = new DataBank.Cli.Models.EntryMetadata();
        RcParser.DetectFormatSpecifiers("Value: %s, Count: %d, Price: %.2f", metadata);

        Assert.Equal(3, metadata.FormatSpecifiers.Count);
        Assert.Contains("%s", metadata.FormatSpecifiers);
        Assert.Contains("%d", metadata.FormatSpecifiers);
        Assert.Contains("%.2f", metadata.FormatSpecifiers);
    }

    [Fact]
    public void DetectFormatSpecifiers_NoSpecifiers_NotBehavioral()
    {
        var metadata = new DataBank.Cli.Models.EntryMetadata();
        RcParser.DetectFormatSpecifiers("Hello World", metadata);

        Assert.Empty(metadata.FormatSpecifiers);
    }

    [Fact]
    public void ExtractQuotedString_SimpleQuoted_ReturnsContent()
    {
        var result = RcParser.ExtractQuotedString("CAPTION \"Hello World\"", "CAPTION".Length);
        Assert.Equal("Hello World", result);
    }

    [Fact]
    public void ExtractQuotedString_EscapedQuotes_ReturnsUnescaped()
    {
        var result = RcParser.ExtractQuotedString("CAPTION \"Hello \"\"World\"\"\"", "CAPTION".Length);
        Assert.Equal("Hello \"World\"", result);
    }

    [Fact]
    public void ExtractQuotedString_UnclosedQuote_ReturnsNull()
    {
        var result = RcParser.ExtractQuotedString("CAPTION \"Hello World", "CAPTION".Length);
        Assert.Null(result);
    }

    [Fact]
    public void ExtractQuotedString_EmptyQuoted_ReturnsEmpty()
    {
        var result = RcParser.ExtractQuotedString("CAPTION \"\"", "CAPTION".Length);
        Assert.Equal("", result);
    }

    [Fact]
    public void Parse_CaptionWithEscapedQuotes_ParsesCorrectly()
    {
        var rcContent = @"
#include ""resource.h""
IDD_ABOUTBOX DIALOGEX 0, 0, 320, 200
CAPTION ""About """"DeltaV"""" Configuration""
LTEXT ""Info"", IDC_STATIC, 10, 10, 200, 15
";
        var entries = ParseRcString(rcContent);

        var caption = entries.First(e => e.Key == "CAPTION::IDD_ABOUTBOX");
        Assert.Equal("About \"DeltaV\" Configuration", caption.Value);
    }

    [Fact]
    public void Parse_EnAndTranslated_SameDialogName_ProducesMatchingKeys()
    {
        var enRc = @"
#include ""resource.h""
IDD_ABOUTBOX DIALOGEX 0, 0, 320, 200
CAPTION ""About DeltaV""
LTEXT ""Version 5.2"", IDC_STATIC, 10, 10, 200, 15
PUSHBUTTON ""OK"", IDOK, 280, 5, 30, 14
";
        var translatedRc = @"
#include ""resource.h""
IDD_ABOUTBOX DIALOGEX 0, 0, 320, 200
CAPTION ""About DeltaV Workstation""
LTEXT ""Version 5.2"", IDC_STATIC, 10, 10, 200, 15
PUSHBUTTON ""OK"", IDOK, 280, 5, 30, 14
";
        var enEntries = ParseRcString(enRc);
        var translatedEntries = ParseRcString(translatedRc);

        var enKeys = enEntries.Select(e => e.Key).OrderBy(k => k).ToList();
        var translatedKeys = translatedEntries.Select(e => e.Key).OrderBy(k => k).ToList();

        Assert.Equal(enKeys, translatedKeys);
    }

    [Fact]
    public void Parse_MultipleIdcStatic_UsesPositionalIndex()
    {
        var rc = """
            LANGUAGE LANG_ENGLISH, SUBLANG_ENGLISH_US

            IDD_TEST DIALOGEX 0, 0, 320, 200
            CAPTION "Test Dialog"
            BEGIN
                LTEXT           "First",IDC_STATIC,10,10,100,15
                LTEXT           "Second",IDC_STATIC,10,30,100,15
                PUSHBUTTON      "OK",IDOK,280,5,30,14
            END
            """;
        var entries = ParseRcString(rc);

        var ltexts = entries.Where(e => e.Key.StartsWith("LTEXT")).ToList();
        Assert.Equal(2, ltexts.Count);

        var key1 = ltexts[0].Key;
        var key2 = ltexts[1].Key;
        Assert.NotEqual(key1, key2);
        Assert.Contains("::0", key1);
        Assert.Contains("::1", key2);
    }

    [Fact]
    public void Parse_ControlElement_WithIdcStatic_UsesPositionalIndex()
    {
        var rc = """
            LANGUAGE LANG_ENGLISH, SUBLANG_ENGLISH_US

            IDD_TEST DIALOGEX 0, 0, 320, 200
            BEGIN
                LTEXT           "Label",IDC_STATIC,10,10,100,15
                CONTROL         "Checkbox",IDC_STATIC,"Static",SS_BLACKFRAME,10,30,100,15
                PUSHBUTTON      "OK",IDOK,280,5,30,14
            END
            """;
        var entries = ParseRcString(rc);

        var idcStaticEntries = entries.Where(e => e.Key.Contains("IDC_STATIC")).ToList();
        Assert.Equal(2, idcStaticEntries.Count);

        var ltextKey = idcStaticEntries.First(e => e.Key.StartsWith("LTEXT")).Key;
        var controlKey = idcStaticEntries.First(e => e.Key.StartsWith("CONTROL")).Key;

        Assert.Contains("::0", ltextKey);
        Assert.Contains("::1", controlKey);
        Assert.NotEqual(ltextKey, controlKey);
    }

    private static List<DataBank.Cli.Models.RawLocalizedEntry> ParseRcString(
        string rcContent, Dictionary<int, string>? symbolMap = null)
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, rcContent);
            return RcParser.Parse(tempFile, symbolMap);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Parse_DntFilename_AllEntriesMarkedDoNotTranslate()
    {
        var rc = """
            LANGUAGE LANG_ENGLISH, SUBLANG_ENGLISH_US

            IDD_TEST DIALOGEX 0, 0, 200, 100
            BEGIN
                LTEXT           "Hello",IDC_STATIC,10,10,100,14
                PUSHBUTTON      "OK",IDOK,10,30,50,14
            END
            """;
        var tempFile = Path.GetTempFileName();
        try
        {
            // Rename to have DNT in filename
            var dntFile = Path.ChangeExtension(tempFile, "-DNT.rc");
            File.Move(tempFile, dntFile);
            File.WriteAllText(dntFile, rc);

            var entries = RcParser.Parse(dntFile);

            Assert.Equal(2, entries.Count);
            Assert.All(entries, e => Assert.True(e.Metadata.DoNotTranslate));
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
            if (File.Exists(Path.ChangeExtension(tempFile, "-DNT.rc")))
                File.Delete(Path.ChangeExtension(tempFile, "-DNT.rc"));
        }
    }
}
