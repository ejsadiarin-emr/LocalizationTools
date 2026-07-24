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
}
