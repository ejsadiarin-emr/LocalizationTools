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

        var rules = new Dictionary<string, RuleMetadata>();
        if (run.TryGetProperty("tool", out var tool) &&
            tool.TryGetProperty("driver", out var driver) &&
            driver.TryGetProperty("rules", out var rulesEl) &&
            rulesEl.ValueKind == JsonValueKind.Array)
        {
            foreach (var rule in rulesEl.EnumerateArray())
            {
                var ruleId = rule.TryGetProperty("id", out var idEl) ? idEl.GetString() ?? "" : "";
                if (string.IsNullOrEmpty(ruleId)) continue;

                var shortDesc = rule.TryGetProperty("shortDescription", out var sd) &&
                    sd.TryGetProperty("text", out var sdText) ? sdText.GetString() ?? "" : "";
                var fullDesc = rule.TryGetProperty("fullDescription", out var fd) &&
                    fd.TryGetProperty("text", out var fdText) ? fdText.GetString() ?? "" : "";
                var helpUri = rule.TryGetProperty("helpUri", out var hu) ? hu.GetString() : null;

                var tags = new List<string>();
                if (rule.TryGetProperty("properties", out var ruleProps) &&
                    ruleProps.TryGetProperty("tags", out var tagsEl) &&
                    tagsEl.ValueKind == JsonValueKind.Array)
                {
                    foreach (var tag in tagsEl.EnumerateArray())
                        tags.Add(tag.GetString() ?? "");
                }

                var relatedRules = new List<string>();
                if (ruleProps.ValueKind == JsonValueKind.Object &&
                    ruleProps.TryGetProperty("relatedRules", out var rrEl) &&
                    rrEl.ValueKind == JsonValueKind.Array)
                {
                    foreach (var rr in rrEl.EnumerateArray())
                        relatedRules.Add(rr.GetString() ?? "");
                }

                string? exampleBad = null;
                string? exampleGood = null;
                if (ruleProps.ValueKind == JsonValueKind.Object &&
                    ruleProps.TryGetProperty("example", out var exEl) &&
                    exEl.ValueKind == JsonValueKind.Object)
                {
                    if (exEl.TryGetProperty("bad", out var badEl)) exampleBad = badEl.GetString();
                    if (exEl.TryGetProperty("good", out var goodEl)) exampleGood = goodEl.GetString();
                }

                rules[ruleId] = new RuleMetadata
                {
                    RuleId = ruleId,
                    ShortDescription = shortDesc,
                    FullDescription = fullDesc,
                    HelpUri = helpUri,
                    Tags = tags,
                    RelatedRules = relatedRules,
                    ExampleBad = exampleBad,
                    ExampleGood = exampleGood
                };
            }
        }

        var parsedResults = new List<SarifResult>();
        if (results.ValueKind == JsonValueKind.Array)
        {
            foreach (var r in results.EnumerateArray())
            {
                var location = r.GetProperty("locations")[0]
                    .GetProperty("physicalLocation");
                var region = location.GetProperty("region");
                var artifact = location.GetProperty("artifactLocation");

                string? classification = null;
                string? sourceSnippet = null;
                string? stringLiteral = null;

                if (r.TryGetProperty("properties", out var resultProps))
                {
                    if (resultProps.TryGetProperty("classification", out var classEl))
                        classification = classEl.GetString();
                    if (resultProps.TryGetProperty("sourceSnippet", out var snippetEl))
                        sourceSnippet = snippetEl.GetString();
                    if (resultProps.TryGetProperty("stringLiteral", out var literalEl))
                        stringLiteral = literalEl.GetString();
                }

                parsedResults.Add(new SarifResult
                {
                    RuleId = r.GetProperty("ruleId").GetString() ?? "",
                    Level = r.GetProperty("level").GetString() ?? "warning",
                    Message = r.GetProperty("message").GetProperty("text").GetString() ?? "",
                    FilePath = artifact.GetProperty("uri").GetString()?.Replace("file:///", "") ?? "",
                    StartLine = region.GetProperty("startLine").GetInt32(),
                    StartColumn = region.GetProperty("startColumn").GetInt32(),
                    EndLine = region.GetProperty("endLine").GetInt32(),
                    EndColumn = region.GetProperty("endColumn").GetInt32(),
                    Classification = classification ?? "",
                    SourceSnippet = sourceSnippet,
                    StringLiteral = stringLiteral
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
            Rules = rules,
            RawSarif = sarifJson
        };
    }
}

public class AnalysisResult
{
    public List<SarifResult> Results { get; set; } = new();
    public List<FileMetric> FileMetrics { get; set; } = new();
    public AnalysisSummary Summary { get; set; } = new();
    public Dictionary<string, RuleMetadata> Rules { get; set; } = new();
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
    public string Classification { get; set; } = "";
    public string? SourceSnippet { get; set; }
    public string? StringLiteral { get; set; }
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

public class RuleMetadata
{
    public string RuleId { get; set; } = "";
    public string ShortDescription { get; set; } = "";
    public string FullDescription { get; set; } = "";
    public string? HelpUri { get; set; }
    public List<string> Tags { get; set; } = new();
    public List<string> RelatedRules { get; set; } = new();
    public string? ExampleBad { get; set; }
    public string? ExampleGood { get; set; }
}
