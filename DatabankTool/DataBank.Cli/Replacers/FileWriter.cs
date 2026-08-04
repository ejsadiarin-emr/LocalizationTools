using System.Text;
using DataBank.Cli.Helpers;
using DataBank.Cli.Models;

namespace DataBank.Cli.Replacers;

public class FileWriter
{
    public EditResult EditEntry(RawLocalizedEntry entry, string newValue)
    {
        var filePath = entry.Source.File;

        if (!File.Exists(filePath))
        {
            return new EditResult
            {
                Success = false,
                ErrorMessage = $"File not found: {filePath}"
            };
        }

        var replacer = FormatReplacerFactory.GetReplacer(entry.Source.Format);
        if (replacer is null)
        {
            return new EditResult
            {
                Success = false,
                ErrorMessage = $"No format replacer available for format: {entry.Source.Format}"
            };
        }

        if (entry.Source.Line is null or <= 0)
        {
            return new EditResult
            {
                Success = false,
                ErrorMessage = "Entry has no valid line number for targeted edit"
            };
        }

        var (content, encoding, lineEnding) = EncodingDetector.ReadFileWithMetadata(filePath);

        var lines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

        var lineIndex = entry.Source.Line.Value - 1;
        if (lineIndex >= lines.Length)
        {
            return new EditResult
            {
                Success = false,
                ErrorMessage = $"Line {entry.Source.Line} does not exist in file (file has {lines.Length} lines)"
            };
        }

        var targetLine = lines[lineIndex];

        var newLine = replacer.ReplaceLine(targetLine, entry.Value, newValue);
        if (newLine is null)
        {
            return new EditResult
            {
                Success = false,
                ErrorMessage = $"Old value not found at line {entry.Source.Line}. Expected: \"{entry.Value}\""
            };
        }

        lines[lineIndex] = newLine;

        var newContent = string.Join(lineEnding, lines);

        // Preserve original BOM behavior: write BOM only if the original file had one
        var hasBom = EncodingDetector.HasBom(filePath);
        var preamble = hasBom ? encoding.GetPreamble() : Array.Empty<byte>();

        using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
        if (preamble.Length > 0)
            stream.Write(preamble, 0, preamble.Length);

        var bytes = encoding.GetBytes(newContent);
        stream.Write(bytes, 0, bytes.Length);

        return new EditResult
        {
            Success = true,
            OldValue = entry.Value,
            NewValue = newValue,
            Line = entry.Source.Line.Value,
            File = filePath
        };
    }
}

public class EditResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public int? Line { get; set; }
    public string? File { get; set; }
}
