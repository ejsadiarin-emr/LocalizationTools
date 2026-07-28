using DataBank.Cli.Models;

namespace DataBank.Api.Services;

public interface IDataBankService
{
    IReadOnlyList<LocalizedStringEntry> GetAllEntries();
    LocalizedStringEntry? GetById(string id);
    LocalizedStringEntry AddEntry(LocalizedStringEntry entry);
    bool UpdateEntry(string id, LocalizedStringEntry entry);
    bool DeleteEntry(string id);
    void AddEntries(IEnumerable<LocalizedStringEntry> entries);
    bool IsDataLoaded { get; }
    void ReloadData();
}
