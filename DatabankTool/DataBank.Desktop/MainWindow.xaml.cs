using System.Configuration;
using System.IO;
using System.Text.Json;
using System.Windows;
using Microsoft.Win32;
using Microsoft.Web.WebView2.Core;

namespace DataBank.Desktop;

public partial class MainWindow : Window
{
    private readonly ApiClient _apiClient;
    private bool _isRemoteMode;
    private string _apiBaseUrl = "http://localhost:5000";

    public MainWindow()
    {
        InitializeComponent();
        _apiClient = new ApiClient(_apiBaseUrl);
        Loaded += MainWindow_Loaded;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        var savedMode = Properties.Settings.Default.AppMode;
        if (savedMode == "Remote")
        {
            RemoteModeRadio.IsChecked = true;
        }

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
                    case "connectApi":
                        HandleConnectApi();
                        break;
                    case "retryConnection":
                        HandleRetryConnection();
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

    private void ConnectApiBtn_Click(object sender, RoutedEventArgs e)
    {
        HandleConnectApi();
    }

    private void RetryBtn_Click(object sender, RoutedEventArgs e)
    {
        HandleRetryConnection();
    }

    private void SwitchToLocalBtn_Click(object sender, RoutedEventArgs e)
    {
        LocalModeRadio.IsChecked = true;
    }

    private void ModeRadio_Checked(object sender, RoutedEventArgs e)
    {
        if (!IsLoaded) return;

        _isRemoteMode = RemoteModeRadio.IsChecked == true;

        Properties.Settings.Default.AppMode = _isRemoteMode ? "Remote" : "Local";
        Properties.Settings.Default.Save();

        LoadJsonBtn.Visibility = _isRemoteMode ? Visibility.Collapsed : Visibility.Visible;
        ConnectApiBtn.Visibility = _isRemoteMode ? Visibility.Visible : Visibility.Collapsed;
        RetryBtn.Visibility = Visibility.Collapsed;
        SwitchToLocalBtn.Visibility = Visibility.Collapsed;

        if (_isRemoteMode)
        {
            HandleConnectApi();
        }
        else
        {
            StatusText.Text = "Local mode - Load a JSON file to start";
        }
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

    private async void HandleConnectApi()
    {
        try
        {
            StatusText.Text = "Connecting to API...";
            ConnectApiBtn.IsEnabled = false;

            var (isHealthy, entryCount, version) = await _apiClient.CheckHealthAsync();

            if (isHealthy)
            {
                StatusText.Text = $"Connected to API - {entryCount} entries (v{version})";
                RetryBtn.Visibility = Visibility.Collapsed;
                SwitchToLocalBtn.Visibility = Visibility.Collapsed;

                await LoadEntriesFromApi();
            }
            else
            {
                StatusText.Text = "API unreachable";
                RetryBtn.Visibility = Visibility.Visible;
                SwitchToLocalBtn.Visibility = Visibility.Visible;
            }
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Connection error: {ex.Message}";
            RetryBtn.Visibility = Visibility.Visible;
            SwitchToLocalBtn.Visibility = Visibility.Visible;
        }
        finally
        {
            ConnectApiBtn.IsEnabled = true;
        }
    }

    private async void HandleRetryConnection()
    {
        await LoadEntriesFromApi();
    }

    private async Task LoadEntriesFromApi()
    {
        try
        {
            StatusText.Text = "Fetching entries...";
            var entries = await _apiClient.FetchEntriesAsync();

            StatusText.Text = $"Loaded {entries.Count} entries from API";

            var payload = JsonSerializer.Serialize(new
            {
                action = "loadData",
                entries = JsonDocument.Parse(JsonSerializer.Serialize(entries)).RootElement
            });
            var escapedPayload = JsonSerializer.Serialize(payload);
            await WebView.CoreWebView2.ExecuteScriptAsync($"window.receiveDataFromCSharp(JSON.parse({escapedPayload}))");
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Failed to load entries: {ex.Message}";
            RetryBtn.Visibility = Visibility.Visible;
            SwitchToLocalBtn.Visibility = Visibility.Visible;
        }
    }
}
