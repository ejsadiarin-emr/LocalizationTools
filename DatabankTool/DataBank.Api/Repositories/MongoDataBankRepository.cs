using DataBank.Api.Models;
using MongoDB.Bson;
using MongoDB.Driver;

namespace DataBank.Api.Repositories;

public class MongoDataBankRepository : IDataBankRepository
{
    private readonly IMongoCollection<DataBankEntryDocument> _entries;
    private readonly IMongoCollection<DataBankMetadataDocument> _metadata;
    private readonly IMongoCollection<TranslationSessionDocument> _sessions;

    public MongoDataBankRepository(IMongoDatabase database)
    {
        _entries = database.GetCollection<DataBankEntryDocument>("DataBankEntry");
        _metadata = database.GetCollection<DataBankMetadataDocument>("DataBankMetadata");
        _sessions = database.GetCollection<TranslationSessionDocument>("TranslationSession");
        EnsureIndexes();
    }

    private void EnsureIndexes()
    {
        var entryKeys = Builders<DataBankEntryDocument>.IndexKeys;
        _entries.Indexes.CreateOne(new CreateIndexModel<DataBankEntryDocument>(
            entryKeys.Ascending(e => e.Key),
            new CreateIndexOptions { Unique = true }));
        _entries.Indexes.CreateOne(new CreateIndexModel<DataBankEntryDocument>(
            entryKeys.Ascending(e => e.Locale)));
        _entries.Indexes.CreateOne(new CreateIndexModel<DataBankEntryDocument>(
            entryKeys.Ascending(e => e.Source.Format)));
        _entries.Indexes.CreateOne(new CreateIndexModel<DataBankEntryDocument>(
            entryKeys.Ascending(e => e.Metadata.DoNotTranslate)));

        var sessionKeys = Builders<TranslationSessionDocument>.IndexKeys;
        _sessions.Indexes.CreateOne(new CreateIndexModel<TranslationSessionDocument>(
            sessionKeys.Ascending(s => s.Status)));
        _sessions.Indexes.CreateOne(new CreateIndexModel<TranslationSessionDocument>(
            sessionKeys.Ascending(s => s.SourceLocale).Ascending(s => s.TargetLocale)));
    }

    public async Task<List<DataBankEntryDocument>> GetAllEntriesAsync()
    {
        return await _entries.Find(_ => true).ToListAsync();
    }

    public async Task<List<DataBankEntryDocument>> GetFilteredEntriesAsync(string? locale, string? format, string? key)
    {
        var filter = Builders<DataBankEntryDocument>.Filter;
        var filters = new List<FilterDefinition<DataBankEntryDocument>>();

        if (!string.IsNullOrEmpty(locale))
            filters.Add(filter.Eq(e => e.Locale, locale));

        if (!string.IsNullOrEmpty(format))
            filters.Add(filter.Eq(e => e.Source.Format, format));

        if (!string.IsNullOrEmpty(key))
            filters.Add(filter.Regex(e => e.Key, new BsonRegularExpression(key, "i")));

        var combinedFilter = filters.Count > 0
            ? filter.And(filters)
            : filter.Empty;

        return await _entries.Find(combinedFilter).ToListAsync();
    }

    public async Task<DataBankEntryDocument?> GetEntryByIdAsync(string id)
    {
        return await _entries.Find(e => e.Id == id).FirstOrDefaultAsync();
    }

    public async Task<DataBankEntryDocument?> GetEntryByKeyAsync(string key)
    {
        return await _entries.Find(e => e.Key == key).FirstOrDefaultAsync();
    }

    public async Task<List<DataBankEntryDocument>> GetEntriesByLocaleAsync(string locale)
    {
        return await _entries.Find(e => e.Locale == locale).ToListAsync();
    }

    public async Task<DataBankEntryDocument> CreateEntryAsync(DataBankEntryDocument entry)
    {
        await _entries.InsertOneAsync(entry);
        return entry;
    }

    public async Task InsertManyEntriesAsync(List<DataBankEntryDocument> entries)
    {
        if (entries.Count == 0) return;
        await _entries.InsertManyAsync(entries);
    }

    public async Task<bool> UpdateEntryAsync(string id, DataBankEntryDocument entry)
    {
        var result = await _entries.ReplaceOneAsync(e => e.Id == id, entry);
        return result.IsAcknowledged && result.ModifiedCount > 0;
    }

    public async Task<bool> DeleteEntryAsync(string id)
    {
        var result = await _entries.DeleteOneAsync(e => e.Id == id);
        return result.IsAcknowledged && result.DeletedCount > 0;
    }

    public async Task<long> GetEntryCountAsync(string? locale = null)
    {
        if (string.IsNullOrEmpty(locale))
            return await _entries.CountDocumentsAsync(_ => true);

        return await _entries.CountDocumentsAsync(e => e.Locale == locale);
    }

    public async Task<long> GetUniqueKeyCountAsync()
    {
        var pipeline = new[]
        {
            new BsonDocument("$group", new BsonDocument { { "_id", "$Key" } }),
            new BsonDocument("$count", "total")
        };
        var result = await _entries.Aggregate<BsonDocument>(pipeline).FirstOrDefaultAsync();
        return result?["total"].ToInt64() ?? 0;
    }

    public async Task<Dictionary<string, long>> GetEntryCountByLocaleAsync()
    {
        var pipeline = new[]
        {
            new BsonDocument("$group", new BsonDocument
            {
                { "_id", "$Locale" },
                { "count", new BsonDocument("$sum", 1) }
            })
        };
        var results = await _entries.Aggregate<BsonDocument>(pipeline).ToListAsync();
        return results.ToDictionary(r => r["_id"].AsString, r => r["count"].ToInt64());
    }

    public async Task<Dictionary<string, long>> GetEntryCountByFormatAsync()
    {
        var pipeline = new[]
        {
            new BsonDocument("$group", new BsonDocument
            {
                { "_id", "$Source.Format" },
                { "count", new BsonDocument("$sum", 1) }
            })
        };
        var results = await _entries.Aggregate<BsonDocument>(pipeline).ToListAsync();
        return results.ToDictionary(r => r["_id"].AsString, r => r["count"].ToInt64());
    }

    public async Task<Dictionary<string, long>> GetTranslationStatusCountsAsync()
    {
        var pipeline = new[]
        {
            new BsonDocument("$group", new BsonDocument
            {
                { "_id", "$Metadata.TranslationStatus" },
                { "count", new BsonDocument("$sum", 1) }
            })
        };
        var results = await _entries.Aggregate<BsonDocument>(pipeline).ToListAsync();
        return results.ToDictionary(r => r["_id"].AsString, r => r["count"].ToInt64());
    }

    public async Task<Dictionary<string, Dictionary<string, long>>> GetTranslationStatusCountsByLocaleAsync()
    {
        var pipeline = new[]
        {
            new BsonDocument("$group", new BsonDocument
            {
                { "_id", new BsonDocument
                    {
                        { "locale", "$Locale" },
                        { "status", "$Metadata.TranslationStatus" }
                    }
                },
                { "count", new BsonDocument("$sum", 1) }
            })
        };
        var results = await _entries.Aggregate<BsonDocument>(pipeline).ToListAsync();
        var dict = new Dictionary<string, Dictionary<string, long>>();
        foreach (var r in results)
        {
            var locale = r["_id"]["locale"].AsString;
            var status = r["_id"]["status"].AsString;
            var count = r["count"].ToInt64();
            if (!dict.ContainsKey(locale))
                dict[locale] = new Dictionary<string, long>();
            dict[locale][status] = count;
        }
        return dict;
    }

    public async Task<DataBankMetadataDocument?> GetMetadataAsync()
    {
        return await _metadata.Find(_ => true).FirstOrDefaultAsync();
    }

    public async Task UpdateMetadataAsync(DataBankMetadataDocument metadata)
    {
        await _metadata.ReplaceOneAsync(
            m => m.Id == metadata.Id,
            metadata,
            new ReplaceOptions { IsUpsert = true });
    }

    public async Task<List<TranslationSessionDocument>> GetAllSessionsAsync(string? status = null)
    {
        if (string.IsNullOrEmpty(status))
            return await _sessions.Find(_ => true).ToListAsync();

        return await _sessions.Find(s => s.Status == status).ToListAsync();
    }

    public async Task<TranslationSessionDocument?> GetSessionByIdAsync(string id)
    {
        if (!ObjectId.TryParse(id, out var objectId))
            return null;

        return await _sessions.Find(s => s.Id == objectId).FirstOrDefaultAsync();
    }

    public async Task<TranslationSessionDocument> CreateSessionAsync(TranslationSessionDocument session)
    {
        await _sessions.InsertOneAsync(session);
        return session;
    }

    public async Task<bool> UpdateSessionStatusAsync(string id, string status)
    {
        if (!ObjectId.TryParse(id, out var objectId))
            return false;

        var result = await _sessions.UpdateOneAsync(
            s => s.Id == objectId,
            Builders<TranslationSessionDocument>.Update
                .Set(s => s.Status, status)
                .Set(s => s.UpdatedAt, DateTime.UtcNow));
        return result.IsAcknowledged && result.ModifiedCount > 0;
    }

    public async Task<bool> AddEntriesToSessionAsync(string id, List<string> entryIds)
    {
        if (!ObjectId.TryParse(id, out var objectId))
            return false;

        var result = await _sessions.UpdateOneAsync(
            s => s.Id == objectId,
            Builders<TranslationSessionDocument>.Update
                .AddToSetEach(s => s.EntryIds, entryIds)
                .Set(s => s.UpdatedAt, DateTime.UtcNow));
        return result.IsAcknowledged && result.ModifiedCount > 0;
    }

    public async Task<bool> DeleteSessionAsync(string id)
    {
        if (!ObjectId.TryParse(id, out var objectId))
            return false;

        var result = await _sessions.DeleteOneAsync(s => s.Id == objectId);
        return result.IsAcknowledged && result.DeletedCount > 0;
    }
}
