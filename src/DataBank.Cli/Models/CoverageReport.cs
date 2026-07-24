namespace DataBank.Cli.Models;

public class CoverageReport
{
    public string Generated { get; set; } = DateTime.UtcNow.ToString("o");
    public List<FileCoverage> Files { get; set; } = [];
    public CoverageSummary Summary { get; set; } = new();
}

public class FileCoverage
{
    public required string EnFile { get; set; }
    public required string TranslatedFile { get; set; }
    public required string Locale { get; set; }
    public int EnKeyCount { get; set; }
    public int TranslatedKeyCount { get; set; }
    public double CompletionPercentage { get; set; }
    public List<string> MissingKeys { get; set; } = [];
    public List<string> OrphanedKeys { get; set; } = [];
}

public class CoverageSummary
{
    public int TotalEnKeys { get; set; }
    public int TotalTranslatedKeys { get; set; }
    public double OverallCompletionPercentage { get; set; }
    public int TotalMissingKeys { get; set; }
    public int TotalOrphanedKeys { get; set; }
    public int TotalUnmatchedFiles { get; set; }
    public List<LocaleCoverage> ByLocale { get; set; } = [];
}

public class LocaleCoverage
{
    public required string Locale { get; set; }
    public int EnKeys { get; set; }
    public int TranslatedKeys { get; set; }
    public double CompletionPercentage { get; set; }
}
