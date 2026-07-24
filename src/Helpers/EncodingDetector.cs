using System.Text;

namespace DataBank.Cli.Helpers;

public static class EncodingDetector
{
    public static Encoding Detect(string filePath)
    {
        if (!File.Exists(filePath))
            return Encoding.UTF8;

        try
        {
            using var stream = File.OpenRead(filePath);
            if (stream.Length < 2)
                return Encoding.UTF8;

            var b0 = stream.ReadByte();
            var b1 = stream.ReadByte();

            if (b0 == 0xEF && b1 == 0xBB)
                return Encoding.UTF8;

            if (b0 == 0xFF && b1 == 0xFE)
                return Encoding.Unicode; // UTF-16LE

            if (b0 == 0xFE && b1 == 0xFF)
                return Encoding.BigEndianUnicode; // UTF-16BE

            return Encoding.UTF8;
        }
        catch
        {
            return Encoding.UTF8;
        }
    }

    public static string ReadFile(string filePath, string? encodingOverride = null)
    {
        var encoding = encodingOverride is not null
            ? GetEncodingByName(encodingOverride)
            : Detect(filePath);

        return File.ReadAllText(filePath, encoding);
    }

    private static Encoding GetEncodingByName(string name)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        return name.ToLowerInvariant() switch
        {
            "utf-8" or "utf8" => Encoding.UTF8,
            "utf-16le" or "utf16le" or "unicode" => Encoding.Unicode,
            "utf-16be" or "utf16be" => Encoding.BigEndianUnicode,
            "windows-1252" or "cp1252" or "1252" => Encoding.GetEncoding(1252),
            "cp936" or "gb2312" or "gbk" => Encoding.GetEncoding(936),
            _ => Encoding.GetEncoding(name)
        };
    }
}
