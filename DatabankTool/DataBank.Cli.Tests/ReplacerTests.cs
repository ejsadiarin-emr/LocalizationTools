using DataBank.Cli.Replacers;

namespace DataBank.Cli.Tests;

public class ReplacerTests
{
    [Fact]
    public void RcReplacer_StringTable_BasicReplace()
    {
        var replacer = new RcReplacer();
        var line = "IDS_WELCOME \"Welcome\"";
        var result = replacer.ReplaceLine(line, "Welcome", "Bonjour");
        Assert.Equal("IDS_WELCOME \"Bonjour\"", result);
    }

    [Fact]
    public void RcReplacer_WithLPrefix_PreservesPrefix()
    {
        var replacer = new RcReplacer();
        var line = "L\"Welcome\"";
        var result = replacer.ReplaceLine(line, "Welcome", "Bonjour");
        Assert.Equal("L\"Bonjour\"", result);
    }

    [Fact]
    public void RcReplacer_EscapedQuotes_HandlesCorrectly()
    {
        var replacer = new RcReplacer();
        var line = "IDS_SAY \"Say \"\"hello\"\"\"";
        var result = replacer.ReplaceLine(line, "Say \"hello\"", "Say \"goodbye\"");
        Assert.Equal("IDS_SAY \"Say \"\"goodbye\"\"\"", result);
    }

    [Fact]
    public void RcReplacer_DialogCaption_ReplacesCorrectly()
    {
        var replacer = new RcReplacer();
        var line = "CAPTION \"Welcome\"";
        var result = replacer.ReplaceLine(line, "Welcome", "Bienvenue");
        Assert.Equal("CAPTION \"Bienvenue\"", result);
    }

    [Fact]
    public void RcReplacer_DialogControl_ReplacesCorrectly()
    {
        var replacer = new RcReplacer();
        var line = "LTEXT \"Welcome\",IDC_STATIC,1,2,3,4";
        var result = replacer.ReplaceLine(line, "Welcome", "Bienvenue");
        Assert.Equal("LTEXT \"Bienvenue\",IDC_STATIC,1,2,3,4", result);
    }

    [Fact]
    public void RcReplacer_OldValueMismatch_ReturnsNull()
    {
        var replacer = new RcReplacer();
        var line = "IDS_WELCOME \"Welcome\"";
        var result = replacer.ReplaceLine(line, "WrongValue", "Bonjour");
        Assert.Null(result);
    }

    [Fact]
    public void FhxReplacer_BasicReplace()
    {
        var replacer = new FhxReplacer();
        var line = "@Key@\t\"context\"\tOld Value";
        var result = replacer.ReplaceLine(line, "Old Value", "New Value");
        Assert.Equal("@Key@\t\"context\"\tNew Value", result);
    }

    [Fact]
    public void FhxReplacer_OldValueMismatch_ReturnsNull()
    {
        var replacer = new FhxReplacer();
        var line = "@Key@\t\"context\"\tOld Value";
        var result = replacer.ReplaceLine(line, "WrongValue", "New Value");
        Assert.Null(result);
    }

    [Fact]
    public void ResxReplacer_BasicReplace()
    {
        var replacer = new ResxReplacer();
        var line = "    <value>Old Value</value>";
        var result = replacer.ReplaceLine(line, "Old Value", "New Value");
        Assert.Equal("    <value>New Value</value>", result);
    }

    [Fact]
    public void ResxReplacer_XmlEntities_EscapesNewValue()
    {
        var replacer = new ResxReplacer();
        var line = "    <value>Old Value</value>";
        var result = replacer.ReplaceLine(line, "Old Value", "New <Value>");
        Assert.Equal("    <value>New &lt;Value&gt;</value>", result);
    }

    [Fact]
    public void ResxReplacer_OldValueMismatch_ReturnsNull()
    {
        var replacer = new ResxReplacer();
        var line = "    <value>Old Value</value>";
        var result = replacer.ReplaceLine(line, "WrongValue", "New Value");
        Assert.Null(result);
    }

    [Fact]
    public void AhcReplacer_BasicReplace()
    {
        var replacer = new AhcReplacer();
        var line = "  <Content>Old Value</Content>";
        var result = replacer.ReplaceLine(line, "Old Value", "New Value");
        Assert.Equal("  <Content>New Value</Content>", result);
    }

    [Fact]
    public void AhcReplacer_OldValueMismatch_ReturnsNull()
    {
        var replacer = new AhcReplacer();
        var line = "  <Content>Old Value</Content>";
        var result = replacer.ReplaceLine(line, "WrongValue", "New Value");
        Assert.Null(result);
    }

    [Fact]
    public void JsonReplacer_BasicReplace()
    {
        var replacer = new JsonReplacer();
        var line = "  \"key\": \"Old Value\"";
        var result = replacer.ReplaceLine(line, "Old Value", "New Value");
        Assert.Equal("  \"key\": \"New Value\"", result);
    }

    [Fact]
    public void JsonReplacer_EscapesSpecialCharacters()
    {
        var replacer = new JsonReplacer();
        var line = "  \"key\": \"Old Value\"";
        var result = replacer.ReplaceLine(line, "Old Value", "New \"Value\"");
        Assert.Equal("  \"key\": \"New \\\"Value\\\"\"", result);
    }

    [Fact]
    public void JsonReplacer_EscapesNewlines()
    {
        var replacer = new JsonReplacer();
        var line = "  \"key\": \"Old Value\"";
        var result = replacer.ReplaceLine(line, "Old Value", "Line1\r\nLine2\nLine3");
        Assert.Equal("  \"key\": \"Line1\\r\\nLine2\\nLine3\"", result);
    }

    [Fact]
    public void JsonReplacer_EscapesBackslash()
    {
        var replacer = new JsonReplacer();
        var line = "  \"key\": \"Old Value\"";
        var result = replacer.ReplaceLine(line, "Old Value", "path\\to\\file");
        Assert.Equal("  \"key\": \"path\\\\to\\\\file\"", result);
    }

    [Fact]
    public void JsonReplacer_OldValueMismatch_ReturnsNull()
    {
        var replacer = new JsonReplacer();
        var line = "  \"key\": \"Old Value\"";
        var result = replacer.ReplaceLine(line, "WrongValue", "New Value");
        Assert.Null(result);
    }

    [Fact]
    public void FormatReplacerFactory_ReturnsCorrectReplacer()
    {
        Assert.IsType<RcReplacer>(FormatReplacerFactory.GetReplacer("rc"));
        Assert.IsType<FhxReplacer>(FormatReplacerFactory.GetReplacer("fhx"));
        Assert.IsType<ResxReplacer>(FormatReplacerFactory.GetReplacer("resx"));
        Assert.IsType<AhcReplacer>(FormatReplacerFactory.GetReplacer("ahc"));
        Assert.IsType<JsonReplacer>(FormatReplacerFactory.GetReplacer("json"));
    }

    [Fact]
    public void FormatReplacerFactory_UnknownFormat_ReturnsNull()
    {
        Assert.Null(FormatReplacerFactory.GetReplacer("unknown"));
    }
}
