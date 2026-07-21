using System.Text.Json;

namespace LocalizationAnalyzers.Desktop.Services;

public class AnalyzerService
{
    public async Task<string> AnalyzeAsync(string projectPath)
    {
        return await Task.Run(() =>
        {
            var sarifLog = SarifCli.AnalyzeProject(projectPath, new[] { projectPath });
            var json = JsonSerializer.Serialize(sarifLog, new JsonSerializerOptions { WriteIndented = true });
            return json;
        });
    }
}
