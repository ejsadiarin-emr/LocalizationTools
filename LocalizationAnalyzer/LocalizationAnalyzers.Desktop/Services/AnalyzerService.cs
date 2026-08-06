using System.Text.Json;

namespace LocalizationAnalyzers.Desktop.Services;

public class AnalyzerService
{
    public async Task<string> AnalyzeAsync(string projectPath, bool includeCaRules = false)
    {
        return await Task.Run(() =>
        {
            var sarifLog = SarifCli.AnalyzeProject(projectPath, new[] { projectPath }, includeCaRules);
            var json = JsonSerializer.Serialize(sarifLog, new JsonSerializerOptions { WriteIndented = true });
            return json;
        });
    }
}
