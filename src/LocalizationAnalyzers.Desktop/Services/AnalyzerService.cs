using System.Text.Json;

namespace LocalizationAnalyzers.Desktop.Services;

public class AnalyzerService
{
    public Task<string> AnalyzeAsync(string projectPath)
    {
        var sarifLog = SarifCli.AnalyzeProject(projectPath, new[] { projectPath });
        var json = JsonSerializer.Serialize(sarifLog, new JsonSerializerOptions { WriteIndented = true });
        return Task.FromResult(json);
    }
}
