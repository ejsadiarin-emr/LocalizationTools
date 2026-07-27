using System.Text;
using DataBank.Cli.Helpers;

namespace DataBank.Cli.Tests;

public class EncodingDetectorTests
{
    [Fact]
    public void Detect_Utf8Bom_ReturnsUtf8()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllBytes(tempFile, [0xEF, 0xBB, 0xBF, 0x48, 0x65, 0x6C, 0x6C, 0x6F]);
            var encoding = EncodingDetector.Detect(tempFile);
            Assert.Equal(Encoding.UTF8, encoding);
        }
        finally { File.Delete(tempFile); }
    }

    [Fact]
    public void Detect_Utf16LeBom_ReturnsUnicode()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllBytes(tempFile, [0xFF, 0xFE, 0x48, 0x00, 0x65, 0x00]);
            var encoding = EncodingDetector.Detect(tempFile);
            Assert.Equal(Encoding.Unicode, encoding);
        }
        finally { File.Delete(tempFile); }
    }

    [Fact]
    public void Detect_Utf16BeBom_ReturnsBigEndianUnicode()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllBytes(tempFile, [0xFE, 0xFF, 0x00, 0x48, 0x00, 0x65]);
            var encoding = EncodingDetector.Detect(tempFile);
            Assert.Equal(Encoding.BigEndianUnicode, encoding);
        }
        finally { File.Delete(tempFile); }
    }

    [Fact]
    public void Detect_NoBom_ReturnsUtf8()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllBytes(tempFile, [0x48, 0x65, 0x6C, 0x6C, 0x6F]);
            var encoding = EncodingDetector.Detect(tempFile);
            Assert.Equal(Encoding.UTF8, encoding);
        }
        finally { File.Delete(tempFile); }
    }

    [Fact]
    public void Detect_EmptyFile_ReturnsUtf8()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllBytes(tempFile, []);
            var encoding = EncodingDetector.Detect(tempFile);
            Assert.Equal(Encoding.UTF8, encoding);
        }
        finally { File.Delete(tempFile); }
    }

    [Fact]
    public void Detect_NonexistentFile_ReturnsUtf8()
    {
        var encoding = EncodingDetector.Detect("nonexistent.txt");
        Assert.Equal(Encoding.UTF8, encoding);
    }

    [Fact]
    public void ReadFile_WithEncodingOverride_UsesSpecifiedEncoding()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, "Hello", Encoding.Unicode);
            var content = EncodingDetector.ReadFile(tempFile, "utf-16le");
            Assert.Equal("Hello", content);
        }
        finally { File.Delete(tempFile); }
    }

    [Fact]
    public void ReadFile_WithBom_DetectsEncoding()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllBytes(tempFile, [0xFF, 0xFE, 0x48, 0x00, 0x69, 0x00]);
            var content = EncodingDetector.ReadFile(tempFile);
            Assert.Equal("Hi", content);
        }
        finally { File.Delete(tempFile); }
    }

    [Fact]
    public void Detect_PragmaCodePage936_ReturnsGb2312()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var tempFile = Path.GetTempFileName();
        try
        {
            var content = "#pragma code_page(936)\r\nSTRINGTABLE\r\nBEGIN\r\nIDS_TEST \"Hello\"\r\nEND";
            var utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
            File.WriteAllBytes(tempFile, utf8NoBom.GetBytes(content));
            var encoding = EncodingDetector.Detect(tempFile);
            Assert.Equal(Encoding.GetEncoding(936), encoding);
        }
        finally { File.Delete(tempFile); }
    }

    [Fact]
    public void Detect_PragmaCodePage1252_ReturnsWindows1252()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var tempFile = Path.GetTempFileName();
        try
        {
            var content = "#pragma code_page(1252)\r\nSTRINGTABLE\r\nBEGIN\r\nIDS_TEST \"Hello\"\r\nEND";
            var utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
            File.WriteAllBytes(tempFile, utf8NoBom.GetBytes(content));
            var encoding = EncodingDetector.Detect(tempFile);
            Assert.Equal(Encoding.GetEncoding(1252), encoding);
        }
        finally { File.Delete(tempFile); }
    }

    [Fact]
    public void Detect_PragmaCodePageQuoted_ReturnsCorrectEncoding()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var tempFile = Path.GetTempFileName();
        try
        {
            var content = "#pragma code_page(\"936\")\r\nSTRINGTABLE\r\nBEGIN\r\nIDS_TEST \"Hello\"\r\nEND";
            var utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
            File.WriteAllBytes(tempFile, utf8NoBom.GetBytes(content));
            var encoding = EncodingDetector.Detect(tempFile);
            Assert.Equal(Encoding.GetEncoding(936), encoding);
        }
        finally { File.Delete(tempFile); }
    }

    [Fact]
    public void Detect_BomTakesPrecedenceOverPragma()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            var header = "#pragma code_page(936)\r\n";
            var bom = new byte[] { 0xEF, 0xBB, 0xBF };
            var bodyBytes = Encoding.UTF8.GetBytes(header);
            var allBytes = new byte[bom.Length + bodyBytes.Length];
            Buffer.BlockCopy(bom, 0, allBytes, 0, bom.Length);
            Buffer.BlockCopy(bodyBytes, 0, allBytes, bom.Length, bodyBytes.Length);
            File.WriteAllBytes(tempFile, allBytes);
            var encoding = EncodingDetector.Detect(tempFile);
            Assert.Equal(Encoding.UTF8, encoding);
        }
        finally { File.Delete(tempFile); }
    }
}
