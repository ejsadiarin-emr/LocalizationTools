using System.IO;
using System.Net.Http;
using System.Text.Json;

namespace DataBank.Desktop;

public class ApiClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private string _baseUrl;
    private bool _disposed;

    public ApiClient(string baseUrl = "http://localhost:5000")
    {
        _httpClient = new HttpClient();
        _baseUrl = baseUrl.TrimEnd('/');
    }

    public static ApiClient Create(string baseUrl)
    {
        return new ApiClient(baseUrl);
    }

    public void SetBaseUrl(string baseUrl)
    {
        _baseUrl = baseUrl.TrimEnd('/');
    }

    public async Task<(bool IsHealthy, int EntryCount, int Version, string? BasePath)> CheckHealthAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/health");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                var entryCount = root.GetProperty("entryCount").GetInt32();
                var version = root.GetProperty("version").GetInt32();
                var basePath = root.TryGetProperty("basePath", out var bpProp) && bpProp.ValueKind == JsonValueKind.String
                    ? bpProp.GetString()
                    : null;

                return (true, entryCount, version, basePath);
            }

            return (false, 0, 0, null);
        }
        catch
        {
            return (false, 0, 0, null);
        }
    }

    public async Task<List<JsonElement>> FetchEntriesAsync()
    {
        var response = await _httpClient.GetAsync($"{_baseUrl}/api/entries");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);

        if (doc.RootElement.ValueKind == JsonValueKind.Array)
        {
            return doc.RootElement.EnumerateArray().ToList();
        }

        return [];
    }

    public async Task<(bool Success, int EntryCount, string? Error)> ImportJsonAsync(string filePath)
    {
        try
        {
            var fileBytes = await File.ReadAllBytesAsync(filePath);
            var content = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(fileBytes);
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
            content.Add(fileContent, "file", Path.GetFileName(filePath));

            var response = await _httpClient.PostAsync($"{_baseUrl}/api/import", content);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                var entryCount = root.GetProperty("entryCount").GetInt32();
                return (true, entryCount, null);
            }
            else
            {
                var errorJson = await response.Content.ReadAsStringAsync();
                var errorDoc = JsonDocument.Parse(errorJson);
                var errorMessage = errorDoc.RootElement.TryGetProperty("error", out var errorProp)
                    ? errorProp.GetString()
                    : "Unknown error";

                return (false, 0, errorMessage);
            }
        }
        catch (Exception ex)
        {
            return (false, 0, ex.Message);
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _httpClient.Dispose();
            }
            _disposed = true;
        }
    }
}
