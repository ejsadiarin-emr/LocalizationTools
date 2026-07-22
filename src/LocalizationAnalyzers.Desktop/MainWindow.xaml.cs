using System.IO;
using System.Text.Json;
using System.Windows;
using LocalizationAnalyzers.Desktop.Services;
using Microsoft.Web.WebView2.Core;

namespace LocalizationAnalyzers.Desktop;

public partial class MainWindow : Window
{
    private readonly AnalyzerService _analyzerService = new();
    private string _lastSarifJson = "";

    public MainWindow()
    {
        InitializeComponent();
        Loaded += MainWindow_Loaded;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        var userDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LocalizationAnalyzer",
            "WebView2");
        var env = await CoreWebView2Environment.CreateAsync(null, userDataFolder);
        await WebView.EnsureCoreWebView2Async(env);

        WebView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;

        var htmlPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot", "index.html");
        WebView.CoreWebView2.Navigate($"file:///{htmlPath.Replace('\\', '/')}");
    }

    private async void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        var message = e.WebMessageAsJson;
        using var doc = JsonDocument.Parse(message);
        var root = doc.RootElement;

        var action = root.GetProperty("action").GetString();
        switch (action)
        {
            case "browseFolder":
                var folderDialog = new Microsoft.Win32.OpenFolderDialog
                {
                    Title = "Select Directory to Analyze"
                };
                if (folderDialog.ShowDialog() == true)
                {
                    var folderPath = folderDialog.FolderName;
                    Dispatcher.BeginInvoke(() =>
                    {
                        var browseResult = JsonSerializer.Serialize(new
                        {
                            action = "browseResult",
                            path = folderPath
                        });
                        WebView.CoreWebView2.PostWebMessageAsJson(browseResult);
                    });
                }
                break;

            case "runAnalysis":
                var projectPath = root.GetProperty("projectPath").GetString() ?? "";
                var includeCaRules = root.TryGetProperty("includeCaRules", out var caProp) && caProp.GetBoolean();
                try
                {
                    _lastSarifJson = await _analyzerService.AnalyzeAsync(projectPath, includeCaRules);
                    var parsed = SarifParser.Parse(_lastSarifJson);
                    var resultJson = JsonSerializer.Serialize(new
                    {
                        action = "analysisResult",
                        success = true,
                        results = parsed.Results,
                        fileMetrics = parsed.FileMetrics,
                        summary = parsed.Summary
                    }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
                    WebView.CoreWebView2.PostWebMessageAsJson(resultJson);
                }
                catch (Exception ex)
                {
                    var errorJson = JsonSerializer.Serialize(new
                    {
                        action = "analysisResult",
                        success = false,
                        error = ex.Message
                    });
                    WebView.CoreWebView2.PostWebMessageAsJson(errorJson);
                }
                break;

            case "exportSarif":
                var outputPath = root.GetProperty("outputPath").GetString() ?? "results.sarif";
                try
                {
                    File.WriteAllText(outputPath, _lastSarifJson);
                    var exportResult = JsonSerializer.Serialize(new
                    {
                        action = "exportResult",
                        success = true,
                        path = Path.GetFullPath(outputPath)
                    });
                    WebView.CoreWebView2.PostWebMessageAsJson(exportResult);
                }
                catch (Exception ex)
                {
                    var exportError = JsonSerializer.Serialize(new
                    {
                        action = "exportResult",
                        success = false,
                        error = ex.Message
                    });
                    WebView.CoreWebView2.PostWebMessageAsJson(exportError);
                }
                break;
        }
    }
}
