using System.Text;
using System.Text.RegularExpressions;

namespace DataBank.Cli.Helpers;

public static class EncodingDetector
{
    private static readonly Regex PragmaCodePagePattern = new(@"#pragma\s+code_page\s*\(\s*(\d+|""[^""]+"")\s*\)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

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

            stream.Position = 0;
            var pragmaEncoding = DetectPragmaCodePage(stream);
            if (pragmaEncoding is not null)
                return pragmaEncoding;

            return Encoding.UTF8;
        }
        catch
        {
            return Encoding.UTF8;
        }
    }

    internal static Encoding? DetectPragmaCodePage(Stream stream)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        using var reader = new StreamReader(stream, Encoding.ASCII, detectEncodingFromByteOrderMarks: false, bufferSize: 2048, leaveOpen: true);
        var header = new char[1024];
        var charsRead = reader.Read(header, 0, header.Length);
        var headerText = new string(header, 0, charsRead);

        var match = PragmaCodePagePattern.Match(headerText);
        if (!match.Success)
            return null;

        var codePageStr = match.Groups[1].Value.Trim('"');
        if (int.TryParse(codePageStr, out var codePage))
            return GetEncodingByCodePage(codePage);

        return null;
    }

    internal static Encoding GetEncodingByCodePage(int codePage)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        return codePage switch
        {
            65001 => Encoding.UTF8,
            1200 => Encoding.Unicode,
            1201 => Encoding.BigEndianUnicode,
            1252 => Encoding.GetEncoding(1252),
            936 => Encoding.GetEncoding(936),
            950 => Encoding.GetEncoding(950),
            949 => Encoding.GetEncoding(949),
            932 => Encoding.GetEncoding(932),
            1250 => Encoding.GetEncoding(1250),
            1251 => Encoding.GetEncoding(1251),
            1253 => Encoding.GetEncoding(1253),
            1254 => Encoding.GetEncoding(1254),
            1255 => Encoding.GetEncoding(1255),
            1256 => Encoding.GetEncoding(1256),
            1257 => Encoding.GetEncoding(1257),
            1258 => Encoding.GetEncoding(1258),
            874 => Encoding.GetEncoding(874),
            _ => Encoding.GetEncoding(codePage)
        };
    }

    public static string ReadFile(string filePath, string? encodingOverride = null)
    {
        var encoding = encodingOverride is not null
            ? GetEncodingByName(encodingOverride)
            : Detect(filePath);

        var content = File.ReadAllText(filePath, encoding);

        if (content.Contains('\uFFFD'))
        {
            Console.Error.WriteLine($"Warning: Encoding mismatch detected in {filePath}. Consider using --encoding override.");
        }

        return content;
    }

    public static string DetectLineEnding(string content)
    {
        var crlfCount = 0;
        var lfCount = 0;

        for (var i = 0; i < content.Length; i++)
        {
            if (content[i] == '\r')
            {
                if (i + 1 < content.Length && content[i + 1] == '\n')
                {
                    crlfCount++;
                    i++;
                }
                else
                {
                    crlfCount++;
                }
            }
            else if (content[i] == '\n')
            {
                lfCount++;
            }
        }

        return crlfCount > lfCount ? "\r\n" : "\n";
    }

    public static bool HasBom(string filePath)
    {
        if (!File.Exists(filePath))
            return false;

        try
        {
            using var stream = File.OpenRead(filePath);
            if (stream.Length < 3)
                return false;

            var b0 = stream.ReadByte();
            var b1 = stream.ReadByte();
            var b2 = stream.ReadByte();

            // UTF-8 BOM: EF BB BF
            if (b0 == 0xEF && b1 == 0xBB && b2 == 0xBF)
                return true;

            // UTF-16LE BOM: FF FE
            if (b0 == 0xFF && b1 == 0xFE)
                return true;

            // UTF-16BE BOM: FE FF
            if (b0 == 0xFE && b1 == 0xFF)
                return true;

            return false;
        }
        catch
        {
            return false;
        }
    }

    public static (string content, Encoding encoding, string lineEnding) ReadFileWithMetadata(string filePath, string? encodingOverride = null)
    {
        var encoding = encodingOverride is not null
            ? GetEncodingByName(encodingOverride)
            : Detect(filePath);

        var content = File.ReadAllText(filePath, encoding);

        if (content.Contains('\uFFFD'))
        {
            Console.Error.WriteLine($"Warning: Encoding mismatch detected in {filePath}. Consider using --encoding override.");
        }

        var lineEnding = DetectLineEnding(content);

        return (content, encoding, lineEnding);
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
