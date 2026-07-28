using System.IO;
using System.Text.Json;
using System.Windows;
using Microsoft.Win32;
using Microsoft.Web.WebView2.Core;

namespace DataBank.Desktop;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Loaded += MainWindow_Loaded;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        var userDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DataBank",
            "WebView2");
        var env = await CoreWebView2Environment.CreateAsync(null, userDataFolder);
        await WebView.EnsureCoreWebView2Async(env);

        WebView.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;

        var htmlPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot", "index.html");
        WebView.CoreWebView2.Navigate($"file:///{htmlPath.Replace('\\', '/')}");
    }

    private void CoreWebView2_WebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        try
        {
            var message = e.WebMessageAsJson;
            using var doc = JsonDocument.Parse(message);
            var root = doc.RootElement;

            if (root.TryGetProperty("action", out var action))
            {
                switch (action.GetString())
                {
                    case "loadJson":
                        HandleLoadJson();
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error processing message: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void LoadJsonBtn_Click(object sender, RoutedEventArgs e)
    {
        HandleLoadJson();
    }

    private void HandleLoadJson()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Select DataBank JSON File",
            Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
            DefaultExt = ".json"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                StatusText.Text = "Loading...";
                var json = File.ReadAllText(dialog.FileName);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var data = JsonSerializer.Deserialize<JsonElement>(json, options);

                if (data.TryGetProperty("entries", out var entries))
                {
                    var entryCount = entries.GetArrayLength();
                    StatusText.Text = $"Loaded {entryCount} entries from {Path.GetFileName(dialog.FileName)}";

                    var payload = JsonSerializer.Serialize(new
                    {
                        action = "loadData",
                        entries = entries
                    });
                    var escapedPayload = JsonSerializer.Serialize(payload);
                    WebView.CoreWebView2.ExecuteScriptAsync($"window.receiveDataFromCSharp(JSON.parse({escapedPayload}))");
                }
                else
                {
                    StatusText.Text = "Error: No 'entries' property found in JSON";
                }
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Error: {ex.Message}";
                MessageBox.Show($"Failed to load JSON: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
