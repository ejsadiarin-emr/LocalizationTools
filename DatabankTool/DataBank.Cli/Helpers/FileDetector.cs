namespace DataBank.Cli.Helpers;

public static class FileDetector
{
    private static readonly Dictionary<string, string> ExtensionMap = new(StringComparer.OrdinalIgnoreCase)
    {
        [".resx"] = "resx",
        [".rc"] = "rc",
        [".fhx"] = "fhx",
        [".ahc"] = "ahc",
        [".grf"] = "grf"
    };

    public static string? DetectFormat(string filePath)
    {
        var ext = Path.GetExtension(filePath);
        if (ExtensionMap.TryGetValue(ext, out var format))
            return format;

        if (string.Equals(ext, ".txt", StringComparison.OrdinalIgnoreCase) && IsFhxFile(filePath))
            return "fhx";

        if (string.Equals(ext, ".json", StringComparison.OrdinalIgnoreCase))
            return "json";

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

    private static bool IsFhxFile(string filePath)
    {
        var dir = Path.GetDirectoryName(filePath);
        while (dir is not null)
        {
            if (string.Equals(Path.GetFileName(dir), "FHX", StringComparison.OrdinalIgnoreCase))
                return true;
            dir = Path.GetDirectoryName(dir);
        }
        return false;
    }
}
