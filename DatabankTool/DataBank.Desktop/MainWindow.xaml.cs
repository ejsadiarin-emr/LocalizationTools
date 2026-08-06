using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using DataBank.Cli.Models;
using DataBank.Cli.Replacers;
using Microsoft.Win32;
using Microsoft.Web.WebView2.Core;

namespace DataBank.Desktop;

public partial class MainWindow : Window
{
    private readonly ApiClient _apiClient;
    private bool _isRemoteMode;
    private string _apiBaseUrl = "http://localhost:5000";
    private string? _basePath;
    private string? _dataBankPath;
    private JsonElement? _dataBankJson;

    public MainWindow()
    {
        InitializeComponent();
        _apiClient = new ApiClient(_apiBaseUrl);
        Loaded += MainWindow_Loaded;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        var savedBasePath = Properties.Settings.Default.BasePath;
        if (!string.IsNullOrEmpty(savedBasePath))
        {
            BasePathInput.Text = savedBasePath;
        }

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
                    case "openSourceFile":
                        HandleOpenSourceFile(root);
                        break;
                    case "exportJson":
                        HandleExportJson(root);
                        break;
                    case "writebackEdit":
                        HandleWritebackEdit(root);
                        break;
                    case "persistMetadata":
                        HandlePersistMetadata(root);
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error processing message: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void HandleOpenSourceFile(JsonElement root)
    {
        try
        {
            if (!root.TryGetProperty("filePath", out var filePathProp))
                return;

            var filePath = filePathProp.GetString();
            if (string.IsNullOrEmpty(filePath))
                return;

            var effectiveBasePath = _isRemoteMode
                ? ExpandBasePath(BasePathInput.Text)
                : _basePath;

            var resolvedPath = filePath;
            if (!Path.IsPathRooted(filePath) && !string.IsNullOrEmpty(effectiveBasePath))
            {
                resolvedPath = Path.Combine(effectiveBasePath, filePath);
            }

            if (!File.Exists(resolvedPath))
            {
                StatusText.Text = $"File not found: {resolvedPath}";
                return;
            }

            int? line = null;
            if (root.TryGetProperty("line", out var lineProp) && lineProp.ValueKind == JsonValueKind.Number)
            {
                line = lineProp.GetInt32();
            }

            if (line.HasValue && TryOpenWithVsCode(resolvedPath, line.Value))
            {
                StatusText.Text = $"Opened {Path.GetFileName(filePath)} at line {line} in VS Code";
                return;
            }

            Process.Start(new ProcessStartInfo(resolvedPath) { UseShellExecute = true });
            StatusText.Text = line.HasValue
                ? $"Opened {Path.GetFileName(filePath)} (line {line})"
                : $"Opened {Path.GetFileName(filePath)}";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Failed to open file: {ex.Message}";
        }
    }

    private static bool TryOpenWithVsCode(string filePath, int line)
    {
        try
        {
            var vsCodePath = FindVsCodeExecutable();
            if (vsCodePath == null)
                return false;

            var psi = new ProcessStartInfo
            {
                FileName = vsCodePath,
                Arguments = $"-g \"{filePath}:{line}\"",
                UseShellExecute = false,
                CreateNoWindow = true
            };
            Process.Start(psi);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string? FindVsCodeExecutable()
    {
        var electronPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Programs", "Microsoft VS Code", "bin", "code.cmd");
        if (File.Exists(electronPath))
            return electronPath;

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "where",
                Arguments = "code",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };
            var process = Process.Start(psi);
            if (process != null)
            {
                var output = process.StandardOutput.ReadToEnd().Trim();
                process.WaitForExit();
                if (process.ExitCode == 0 && !string.IsNullOrEmpty(output))
                {
                    var firstLine = output.Split('\n')[0].Trim();
                    if (File.Exists(firstLine))
                        return firstLine;
                }
            }
        }
        catch
        {
        }

        return null;
    }

    private void HandleExportJson(JsonElement root)
    {
        try
        {
            if (!root.TryGetProperty("data", out var dataProp))
                return;

            var jsonString = dataProp.GetString();
            if (string.IsNullOrEmpty(jsonString))
                return;

            var defaultFilename = "databank-export.json";
            if (root.TryGetProperty("defaultFilename", out var filenameProp))
            {
                var fn = filenameProp.GetString();
                if (!string.IsNullOrEmpty(fn))
                    defaultFilename = fn;
            }

            var dialog = new SaveFileDialog
            {
                Title = "Export DataBank JSON",
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                DefaultExt = ".json",
                FileName = defaultFilename
            };

            if (dialog.ShowDialog() == true)
            {
                File.WriteAllText(dialog.FileName, jsonString);
                StatusText.Text = $"Exported to {Path.GetFileName(dialog.FileName)}";
            }
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Export failed: {ex.Message}";
        }
    }

    private async void HandleWritebackEdit(JsonElement root)
    {
        try
        {
            if (!root.TryGetProperty("key", out var keyProp) ||
                !root.TryGetProperty("locale", out var localeProp) ||
                !root.TryGetProperty("oldValue", out var oldValueProp) ||
                !root.TryGetProperty("newValue", out var newValueProp))
            {
                return;
            }

            var key = keyProp.GetString() ?? "";
            var locale = localeProp.GetString() ?? "";
            var oldValue = oldValueProp.GetString() ?? "";
            var newValue = newValueProp.GetString() ?? "";

            if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(locale))
                return;

            string? format = null;
            string? file = null;
            int? line = null;

            if (root.TryGetProperty("format", out var formatProp))
                format = formatProp.GetString();
            if (root.TryGetProperty("file", out var fileProp))
                file = fileProp.GetString();
            if (root.TryGetProperty("line", out var lineProp) && lineProp.ValueKind == JsonValueKind.Number)
                line = lineProp.GetInt32();

            if (string.IsNullOrEmpty(format) || string.IsNullOrEmpty(file) || !line.HasValue)
            {
                PostWritebackResult(new { success = false, error = "No source file information for this value" });
                return;
            }

            var resolvedPath = file;
            var effectiveBasePath = _isRemoteMode
                ? ExpandBasePath(BasePathInput.Text)
                : _basePath;
            if (!Path.IsPathRooted(file) && !string.IsNullOrEmpty(effectiveBasePath))
            {
                resolvedPath = Path.Combine(effectiveBasePath, file);
            }

            var rawEntry = new RawLocalizedEntry
            {
                Key = key,
                Locale = locale,
                Value = oldValue,
                Source = new SourceInfo
                {
                    Format = format,
                    File = resolvedPath,
                    Path = resolvedPath,
                    Line = line
                }
            };

            var result = new FileWriter().EditEntry(rawEntry, newValue);

            if (result.Success)
            {
                if (_isRemoteMode)
                {
                    await PersistEditToRemote(key, locale, newValue);
                }
                else
                {
                    PersistEditToLocal(key, locale, newValue, result.Line);
                }
            }

            PostWritebackResult(new
            {
                success = result.Success,
                error = result.ErrorMessage,
                key,
                locale,
                line = result.Line,
                file = result.File
            });

            StatusText.Text = result.Success
                ? (_isRemoteMode
                    ? $"Saved {key} [{locale}] -> {resolvedPath}:{line} + remote"
                    : $"Saved {key} [{locale}] -> {resolvedPath}:{line} + data-bank.json")
                : $"Write-back failed: {result.ErrorMessage}";
        }
        catch (Exception ex)
        {
            PostWritebackResult(new { success = false, error = ex.Message });
            StatusText.Text = $"Write-back error: {ex.Message}";
        }
    }

    private async void PostWritebackResult(object payload)
    {
        try
        {
            var json = JsonSerializer.Serialize(payload);
            var escaped = JsonSerializer.Serialize(json);
            await WebView.CoreWebView2.ExecuteScriptAsync($"window.receiveWritebackResult(JSON.parse({escaped}))");
        }
        catch
        {
        }
    }

    private void PersistEditToLocal(string key, string locale, string newValue, int? newLine)
    {
        if (_dataBankPath == null || _dataBankJson == null)
            return;

        try
        {
            var data = _dataBankJson.Value;
            if (!data.TryGetProperty("entries", out var entries))
                return;

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var fullJson = data.GetRawText();
            using var doc = JsonDocument.Parse(fullJson);
            var root = doc.RootElement.Clone();

            var updatedRoot = UpdateEntryInJson(root, key, locale, newValue, newLine);
            var writeOptions = new JsonSerializerOptions { WriteIndented = true };
            var serialized = JsonSerializer.Serialize(updatedRoot, writeOptions);
            File.WriteAllText(_dataBankPath, serialized);

            // Update cached JSON
            _dataBankJson = JsonDocument.Parse(serialized).RootElement;
        }
        catch
        {
            StatusText.Text = $"Warning: source file saved but data-bank.json write failed for {key} [{locale}]";
        }
    }

    private static JsonElement UpdateEntryInJson(JsonElement root, string key, string locale, string newValue, int? newLine)
    {
        var entriesJson = root.GetProperty("entries").GetRawText();
        var entriesList = JsonSerializer.Deserialize<List<Dictionary<string, JsonElement>>>(entriesJson)!;

        foreach (var entryDict in entriesList)
        {
            if (entryDict.TryGetValue("key", out var keyElement) && keyElement.GetString() == key)
            {
                // Update values
                if (entryDict.TryGetValue("values", out var valuesElement))
                {
                    var valuesList = JsonSerializer.Deserialize<List<Dictionary<string, JsonElement>>>(valuesElement.GetRawText())!;
                    foreach (var valDict in valuesList)
                    {
                        if (valDict.TryGetValue("locale", out var locElement) && locElement.GetString() == locale)
                        {
                            valDict["value"] = JsonSerializer.SerializeToElement(newValue);
                            break;
                        }
                    }
                    entryDict["values"] = JsonSerializer.SerializeToElement(valuesList);
                }

                // Update sources[locale].line
                if (newLine.HasValue && entryDict.TryGetValue("sources", out var sourcesElement) && sourcesElement.TryGetProperty(locale, out var _))
                {
                    var sourcesDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(sourcesElement.GetRawText())!;
                    if (sourcesDict.TryGetValue(locale, out var srcElement))
                    {
                        var srcDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(srcElement.GetRawText())!;
                        srcDict["line"] = JsonSerializer.SerializeToElement(newLine.Value);
                        sourcesDict[locale] = JsonSerializer.SerializeToElement(srcDict);
                        entryDict["sources"] = JsonSerializer.SerializeToElement(sourcesDict);
                    }
                }

                break;
            }
        }

        var rebuiltRoot = root.Clone();
        var rootDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(rebuiltRoot.GetRawText())!;
        rootDict["entries"] = JsonSerializer.SerializeToElement(entriesList);
        return JsonSerializer.SerializeToElement(rootDict);
    }

    private async Task PersistEditToRemote(string key, string locale, string newValue)
    {
        try
        {
            var success = await _apiClient.UpdateLocaleValueAsync(key, locale, newValue);
            if (!success)
            {
                StatusText.Text = $"Warning: source file saved but remote update failed for {key} [{locale}]";
            }
        }
        catch
        {
            StatusText.Text = $"Warning: source file saved but remote update failed for {key} [{locale}]";
        }
    }

    private async void HandlePersistMetadata(JsonElement root)
    {
        try
        {
            if (!root.TryGetProperty("key", out var keyProp) ||
                !root.TryGetProperty("metadata", out var metadataProp))
            {
                return;
            }

            var key = keyProp.GetString() ?? "";
            if (string.IsNullOrEmpty(key))
                return;

            if (_isRemoteMode)
            {
                // Find the full entry from the API and update it
                var entries = await _apiClient.FetchEntriesAsync();
                var entry = entries.FirstOrDefault(e =>
                    e.TryGetProperty("key", out var k) && k.GetString() == key);
                if (entry.ValueKind != JsonValueKind.Undefined)
                {
                    // Update the metadata in the entry
                    var entryDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(entry.GetRawText())!;
                    entryDict["metadata"] = metadataProp;
                    var updatedEntry = JsonSerializer.SerializeToElement(entryDict);
                    var success = await _apiClient.UpdateEntryAsync(key, updatedEntry);
                    if (!success)
                    {
                        StatusText.Text = $"Warning: metadata update failed for {key}";
                    }
                    else
                    {
                        StatusText.Text = $"Metadata saved for {key}";
                    }
                }
            }
            else
            {
                // Local mode: update _dataBankJson and write to disk
                if (_dataBankPath == null || _dataBankJson == null)
                    return;

                var data = _dataBankJson.Value;
                if (!data.TryGetProperty("entries", out var entries))
                    return;

                var entriesJson = entries.GetRawText();
                var entriesList = JsonSerializer.Deserialize<List<Dictionary<string, JsonElement>>>(entriesJson)!;

                foreach (var entryDict in entriesList)
                {
                    if (entryDict.TryGetValue("key", out var keyElement) && keyElement.GetString() == key)
                    {
                        entryDict["metadata"] = metadataProp;
                        break;
                    }
                }

                var rootDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(data.GetRawText())!;
                rootDict["entries"] = JsonSerializer.SerializeToElement(entriesList);
                var updatedRoot = JsonSerializer.SerializeToElement(rootDict);

                var writeOptions = new JsonSerializerOptions { WriteIndented = true };
                var serialized = JsonSerializer.Serialize(updatedRoot, writeOptions);
                File.WriteAllText(_dataBankPath, serialized);
                _dataBankJson = JsonDocument.Parse(serialized).RootElement;

                StatusText.Text = $"Metadata saved for {key}";
            }
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Metadata save error: {ex.Message}";
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

    private async void ImportBtn_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Select DataBank JSON to Import to API",
            Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
            DefaultExt = ".json"
        };

        if (dialog.ShowDialog() != true) return;

        ImportBtn.IsEnabled = false;
        try
        {
            StatusText.Text = "Importing...";
            var (success, entryCount, error) = await _apiClient.ImportJsonAsync(dialog.FileName);

            if (success)
            {
                StatusText.Text = $"Imported {entryCount} entries from {Path.GetFileName(dialog.FileName)}";
                await LoadEntriesFromApi();
            }
            else
            {
                StatusText.Text = $"Import failed: {error}";
            }
        }
        finally
        {
            ImportBtn.IsEnabled = true;
        }
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
        ImportBtn.Visibility = _isRemoteMode ? Visibility.Visible : Visibility.Collapsed;
        BasePathLabel.Visibility = _isRemoteMode ? Visibility.Visible : Visibility.Collapsed;
        BasePathInput.Visibility = _isRemoteMode ? Visibility.Visible : Visibility.Collapsed;
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

                _basePath = null;
                if (data.TryGetProperty("basePath", out var basePathProp) && basePathProp.ValueKind == JsonValueKind.String)
                {
                    _basePath = basePathProp.GetString();
                }

                _dataBankPath = dialog.FileName;
                _dataBankJson = data;

                if (data.TryGetProperty("entries", out var entries))
                {
                    var entryCount = entries.GetArrayLength();
                    StatusText.Text = $"Loaded {entryCount} entries from {Path.GetFileName(dialog.FileName)}";

                    var payload = JsonSerializer.Serialize(new
                    {
                        action = "loadData",
                        basePath = _basePath ?? "",
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

            var (isHealthy, entryCount, version, apiBasePath) = await _apiClient.CheckHealthAsync();

            if (isHealthy)
            {
                var userBasePath = ExpandBasePath(BasePathInput.Text);
                _basePath = !string.IsNullOrEmpty(userBasePath) ? userBasePath : apiBasePath;
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

    private void BasePathInput_LostFocus(object sender, RoutedEventArgs e)
    {
        Properties.Settings.Default.BasePath = BasePathInput.Text;
        Properties.Settings.Default.Save();

        if (_isRemoteMode)
        {
            var expanded = ExpandBasePath(BasePathInput.Text);
            if (!string.IsNullOrEmpty(expanded))
                _basePath = expanded;
        }
    }

    private static string ExpandBasePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return string.Empty;

        path = path.Trim();

        if (path.StartsWith('~'))
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            path = path.Length == 1 ? home : Path.Combine(home, path[1..].TrimStart('\\', '/'));
        }

        return Path.GetFullPath(path);
    }
}
