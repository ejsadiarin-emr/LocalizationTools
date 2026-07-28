namespace DataBank.Cli.Helpers;

public static class FileHelper
{
    public static bool HasDntInFilename(string filePath)
    {
        var fileName = Path.GetFileName(filePath);
        return fileName.Contains("DNT", StringComparison.OrdinalIgnoreCase);
    }
}