using DataBank.Api.Models;

namespace DataBank.Api.Repositories;

public interface IDataBankRepository
{
    Task<List<DataBankEntryDocument>> GetAllEntriesAsync();
    Task<List<DataBankEntryDocument>> GetFilteredEntriesAsync(string? locale, string? format, string? key);
    Task<DataBankEntryDocument?> GetEntryByIdAsync(string id);
    Task<DataBankEntryDocument?> GetEntryByKeyAsync(string key);
    Task<List<DataBankEntryDocument>> GetEntriesByLocaleAsync(string locale);
    Task<DataBankEntryDocument> CreateEntryAsync(DataBankEntryDocument entry);
    Task InsertManyEntriesAsync(List<DataBankEntryDocument> entries);
    Task<int> ReplaceOrInsertManyAsync(List<DataBankEntryDocument> entries);
    Task<bool> UpdateEntryAsync(string id, DataBankEntryDocument entry);
    Task<bool> DeleteEntryAsync(string id);
    Task<long> GetEntryCountAsync(string? locale = null);
    Task<long> GetUniqueKeyCountAsync();

    Task<Dictionary<string, long>> GetEntryCountByLocaleAsync();
    Task<Dictionary<string, long>> GetEntryCountByFormatAsync();
    Task<Dictionary<string, long>> GetTranslationStatusCountsAsync();
    Task<Dictionary<string, Dictionary<string, long>>> GetTranslationStatusCountsByLocaleAsync();

    Task<DataBankMetadataDocument?> GetMetadataAsync();
    Task UpdateMetadataAsync(DataBankMetadataDocument metadata);

    Task<List<TranslationSessionDocument>> GetAllSessionsAsync(string? status = null);
    Task<TranslationSessionDocument?> GetSessionByIdAsync(string id);
    Task<TranslationSessionDocument> CreateSessionAsync(TranslationSessionDocument session);
    Task<bool> UpdateSessionStatusAsync(string id, string status);
    Task<bool> AddEntriesToSessionAsync(string id, List<string> entryIds);
    Task<bool> DeleteSessionAsync(string id);
}
