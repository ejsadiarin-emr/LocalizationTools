using System.Collections.Concurrent;
using System.Text.Json;
using DataBank.Cli.Models;

namespace DataBank.Api.Services;

public class FileDataBankService : IDataBankService
{
    private readonly string _dataFilePath;
    private readonly ILogger<FileDataBankService> _logger;
    private readonly ConcurrentDictionary<string, LocalizedStringEntry> _entries = new();
    private bool _dataLoaded;

    public bool IsDataLoaded => _dataLoaded;

    public FileDataBankService(IConfiguration configuration, ILogger<FileDataBankService> logger)
    {
        _logger = logger;
        _dataFilePath = configuration["DataBank:DataFilePath"] ?? "data-bank.json";
        LoadData();
    }

    private void LoadData()
    {
        if (!File.Exists(_dataFilePath))
        {
            _logger.LogWarning("Data file not found at {Path}. API will return 503 until data is available.", _dataFilePath);
            return;
        }

        try
        {
            var json = File.ReadAllText(_dataFilePath);
            var output = JsonSerializer.Deserialize<DataBankOutput>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            _entries.Clear();
            foreach (var entry in output?.Entries ?? [])
            {
                _entries[entry.Id] = entry;
            }

            _dataLoaded = true;
            _logger.LogInformation("Loaded {Count} entries from {Path}", _entries.Count, _dataFilePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load data from {Path}", _dataFilePath);
        }
    }

    public IReadOnlyList<LocalizedStringEntry> GetAllEntries()
    {
        return _entries.Values.ToList();
    }

    public LocalizedStringEntry? GetById(string id)
    {
        return _entries.TryGetValue(id, out var entry) ? entry : null;
    }

    public LocalizedStringEntry AddEntry(LocalizedStringEntry entry)
    {
        _entries[entry.Id] = entry;
        SaveData();
        return entry;
    }

    public bool UpdateEntry(string id, LocalizedStringEntry entry)
    {
        if (!_entries.ContainsKey(id))
            return false;

        entry.Id = id;
        _entries[id] = entry;
        SaveData();
        return true;
    }

    public bool DeleteEntry(string id)
    {
        if (!_entries.TryRemove(id, out _))
            return false;

        SaveData();
        return true;
    }

    public void AddEntries(IEnumerable<LocalizedStringEntry> entries)
    {
        foreach (var entry in entries)
        {
            _entries[entry.Id] = entry;
        }
        SaveData();
    }

    public void ReloadData()
    {
        LoadData();
    }

    private void SaveData()
    {
        try
        {
            var output = new DataBankOutput
            {
                Entries = _entries.Values.ToList()
            };
            var json = JsonSerializer.Serialize(output, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_dataFilePath, json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save data to {Path}", _dataFilePath);
        }
    }
}
