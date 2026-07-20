using System.Text.Json;

namespace LocalizationAnalyzers.Desktop.Services;

public class SarifParser
{
    public static AnalysisResult Parse(string sarifJson)
    {
        using var doc = JsonDocument.Parse(sarifJson);
        var root = doc.RootElement;

        var run = root.GetProperty("runs")[0];
        var results = run.TryGetProperty("results", out var resultsEl) ? resultsEl : default;
        var properties = run.TryGetProperty("properties", out var propsEl) ? propsEl : default;
        var invocations = run.TryGetProperty("invocations", out var invEl) ? invEl : default;

        var parsedResults = new List<SarifResult>();
        if (results.ValueKind == JsonValueKind.Array)
        {
            foreach (var r in results.EnumerateArray())
            {
                var location = r.GetProperty("locations")[0]
                    .GetProperty("physicalLocation");
                var region = location.GetProperty("region");
                var artifact = location.GetProperty("artifactLocation");

                parsedResults.Add(new SarifResult
                {
                    RuleId = r.GetProperty("ruleId").GetString() ?? "",
                    Level = r.GetProperty("level").GetString() ?? "warning",
                    Message = r.GetProperty("message").GetProperty("text").GetString() ?? "",
                    FilePath = artifact.GetProperty("uri").GetString()?.Replace("file:///", "") ?? "",
                    StartLine = region.GetProperty("startLine").GetInt32(),
                    StartColumn = region.GetProperty("startColumn").GetInt32(),
                    EndLine = region.GetProperty("endLine").GetInt32(),
                    EndColumn = region.GetProperty("endColumn").GetInt32()
                });
            }
        }

        var fileMetrics = new List<FileMetric>();
        if (propsEl.ValueKind == JsonValueKind.Object && propsEl.TryGetProperty("fileMetrics", out var fmEl) && fmEl.ValueKind == JsonValueKind.Array)
        {
            foreach (var fm in fmEl.EnumerateArray())
            {
                fileMetrics.Add(new FileMetric
                {
                    FilePath = fm.GetProperty("filePath").GetString() ?? "",
                    FileSizeBytes = fm.GetProperty("fileSizeBytes").GetInt64(),
                    LineCount = fm.GetProperty("lineCount").GetInt32(),
                    DiagnosticCount = fm.GetProperty("diagnosticCount").GetInt32()
                });
            }
        }

        var summary = new AnalysisSummary
        {
            TotalFileCount = propsEl.ValueKind == JsonValueKind.Object && propsEl.TryGetProperty("totalFileCount", out var tfc) ? tfc.GetInt32() : 0,
            TotalLineCount = propsEl.ValueKind == JsonValueKind.Object && propsEl.TryGetProperty("totalLineCount", out var tlc) ? tlc.GetInt32() : 0,
            TotalDurationMs = propsEl.ValueKind == JsonValueKind.Object && propsEl.TryGetProperty("totalDurationMs", out var tdm) ? tdm.GetInt64() : 0,
            TotalDiagnostics = parsedResults.Count,
            DiagnosticsByRule = parsedResults.GroupBy(r => r.RuleId)
                .ToDictionary(g => g.Key, g => g.Count()),
            DiagnosticsBySeverity = parsedResults.GroupBy(r => r.Level)
                .ToDictionary(g => g.Key, g => g.Count())
        };

        if (invEl.ValueKind == JsonValueKind.Array && invEl.GetArrayLength() > 0)
        {
            var inv = invEl[0];
            if (inv.TryGetProperty("startTimeUtc", out var st)) summary.StartTimeUtc = st.GetDateTime();
            if (inv.TryGetProperty("endTimeUtc", out var et)) summary.EndTimeUtc = et.GetDateTime();
        }

        return new AnalysisResult
        {
            Results = parsedResults,
            FileMetrics = fileMetrics,
            Summary = summary,
            RawSarif = sarifJson
        };
    }
}

public class AnalysisResult
{
    public List<SarifResult> Results { get; set; } = new();
    public List<FileMetric> FileMetrics { get; set; } = new();
    public AnalysisSummary Summary { get; set; } = new();
    public string RawSarif { get; set; } = "";
}

public class SarifResult
{
    public string RuleId { get; set; } = "";
    public string Level { get; set; } = "";
    public string Message { get; set; } = "";
    public string FilePath { get; set; } = "";
    public int StartLine { get; set; }
    public int StartColumn { get; set; }
    public int EndLine { get; set; }
    public int EndColumn { get; set; }
}

public class FileMetric
{
    public string FilePath { get; set; } = "";
    public long FileSizeBytes { get; set; }
    public int LineCount { get; set; }
    public int DiagnosticCount { get; set; }
}

public class AnalysisSummary
{
    public int TotalFileCount { get; set; }
    public int TotalLineCount { get; set; }
    public long TotalDurationMs { get; set; }
    public int TotalDiagnostics { get; set; }
    public Dictionary<string, int> DiagnosticsByRule { get; set; } = new();
    public Dictionary<string, int> DiagnosticsBySeverity { get; set; } = new();
    public DateTime? StartTimeUtc { get; set; }
    public DateTime? EndTimeUtc { get; set; }
}
