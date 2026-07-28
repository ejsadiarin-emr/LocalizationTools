namespace DataBank.Cli.Helpers;

public static class FileDetector
{
    private static readonly Dictionary<string, string> ExtensionMap = new(StringComparer.OrdinalIgnoreCase)
    {
        [".resx"] = "resx",
        [".rc"] = "rc",
        [".fhx"] = "fhx",
        [".ahc"] = "ahc",
        [".json"] = "json",
        [".grf"] = "grf"
    };

    public static string? DetectFormat(string filePath)
    {
        var ext = Path.GetExtension(filePath);
        if (ExtensionMap.TryGetValue(ext, out var format))
            return format;

        var dirName = Path.GetFileName(Path.GetDirectoryName(filePath) ?? "");
        if (string.Equals(dirName, "Fhx", StringComparison.OrdinalIgnoreCase))
            return "fhx";

        if (string.Equals(ext, ".txt", StringComparison.OrdinalIgnoreCase) && IsFhxContent(filePath))
            return "fhx";

        return null;
    }

    public static List<(string path, string format)> DiscoverFiles(string rootDir, string? formatFilter = null)
    {
        var results = new List<(string path, string format)>();
        if (!Directory.Exists(rootDir))
            return results;

        var allFiles = Directory.GetFiles(rootDir, "*.*", SearchOption.AllDirectories);

        foreach (var file in allFiles)
        {
            var detected = DetectFormat(file);
            if (detected is null)
                continue;

            if (formatFilter is not null && !formatFilter.Equals(detected, StringComparison.OrdinalIgnoreCase))
                continue;

            results.Add((file, detected));
        }

        return results;
    }

    private static bool IsFhxContent(string filePath)
    {
        try
        {
            using var reader = new StreamReader(filePath);
            var firstLine = reader.ReadLine();
            return firstLine is not null && firstLine.StartsWith("@Key@\t");
        }
        catch
        {
            return false;
        }
    }
}
