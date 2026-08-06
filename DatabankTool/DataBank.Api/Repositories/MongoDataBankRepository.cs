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
        // Drop all existing non-_id indexes to handle schema migrations
        try { _entries.Indexes.DropAll(); } catch { }

        // If collection has duplicate Keys (v2 data), drop and recreate the collection
        var count = _entries.CountDocuments(_ => true);
        if (count > 0)
        {
            var distinctKeys = _entries.Distinct(e => e.Key, _ => true).ToList().Count;
            if (distinctKeys < count)
            {
                // Duplicate keys exist — drop collection so CLI re-imports v3 data
                _entries.Database.DropCollection("DataBankEntry");
            }
        }

        var entryKeys = Builders<DataBankEntryDocument>.IndexKeys;
        // Unique compound index on Key + Context (same key can appear with different contexts in FHX)
        _entries.Indexes.CreateOne(new CreateIndexModel<DataBankEntryDocument>(
            entryKeys.Ascending(e => e.Key).Ascending(e => e.Context),
            new CreateIndexOptions { Unique = true }));
        // Index on Values.Locale for locale-based filtering
        _entries.Indexes.CreateOne(new CreateIndexModel<DataBankEntryDocument>(
            entryKeys.Ascending("Values.Locale")));
        // Index on Sources.Format for format-based filtering
        _entries.Indexes.CreateOne(new CreateIndexModel<DataBankEntryDocument>(
            entryKeys.Ascending("Sources.Format")));
        _entries.Indexes.CreateOne(new CreateIndexModel<DataBankEntryDocument>(
            entryKeys.Ascending(e => e.Metadata.DoNotTranslate)));

        var sessionKeys = Builders<TranslationSessionDocument>.IndexKeys;
        try { _sessions.Indexes.DropAll(); } catch { }
        _sessions.Indexes.CreateOne(new CreateIndexModel<TranslationSessionDocument>(
            sessionKeys.Ascending(s => s.Status)));
        _sessions.Indexes.CreateOne(new CreateIndexModel<TranslationSessionDocument>(
            sessionKeys.Ascending(s => s.SourceLocale).Ascending(s => s.TargetLocale)));
    }

    public async Task<List<DataBankEntryDocument>> GetAllEntriesAsync()
    {
        return await _entries.Find(_ => true).ToListAsync();
    }

    public async Task<List<DataBankEntryDocument>> GetFilteredEntriesAsync(string? locale, string? format, string? key, string? context = null)
    {
        var filter = Builders<DataBankEntryDocument>.Filter;
        var filters = new List<FilterDefinition<DataBankEntryDocument>>();

        if (!string.IsNullOrEmpty(locale))
            filters.Add(filter.ElemMatch(e => e.Values, v => v.Locale == locale));

        if (!string.IsNullOrEmpty(format))
            filters.Add(filter.Eq("Sources.Format", format));

        if (!string.IsNullOrEmpty(key))
            filters.Add(filter.Regex(e => e.Key, new BsonRegularExpression(key, "i")));

        if (context is not null)
            filters.Add(filter.Eq(e => e.Context, context));

        var combinedFilter = filters.Count > 0
            ? filter.And(filters)
            : filter.Empty;

        return await _entries.Find(combinedFilter).ToListAsync();
    }

    public async Task<DataBankEntryDocument?> GetEntryByIdAsync(string id)
    {
        return await _entries.Find(e => e.Id == id).FirstOrDefaultAsync();
    }

    public async Task<DataBankEntryDocument?> GetEntryByKeyAsync(string key, string? context = null)
    {
        if (context is null)
            return await _entries.Find(e => e.Key == key && e.Context == null).FirstOrDefaultAsync();

        return await _entries.Find(e => e.Key == key && e.Context == context).FirstOrDefaultAsync();
    }

    public async Task<List<DataBankEntryDocument>> GetEntriesByLocaleAsync(string locale)
    {
        return await _entries.Find(e => e.Values.Any(v => v.Locale == locale)).ToListAsync();
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

    public async Task<int> ReplaceOrInsertManyAsync(List<DataBankEntryDocument> entries)
    {
        if (entries.Count == 0) return 0;

        const int batchSize = 1000;
        int totalReplaced = 0;

        for (int i = 0; i < entries.Count; i += batchSize)
        {
            var batch = entries.Skip(i).Take(batchSize).ToList();
            var models = batch.Select(entry => new ReplaceOneModel<DataBankEntryDocument>(
                Builders<DataBankEntryDocument>.Filter.And(
                    Builders<DataBankEntryDocument>.Filter.Eq(e => e.Key, entry.Key),
                    Builders<DataBankEntryDocument>.Filter.Eq(e => e.Context, entry.Context)),
                entry)
            {
                IsUpsert = true
            }).ToList();

            var result = await _entries.BulkWriteAsync(models);
            totalReplaced += (int)(result.Upserts.Count + result.ModifiedCount);
        }

        return totalReplaced;
    }

    public async Task<bool> UpdateEntryAsync(string id, DataBankEntryDocument entry)
    {
        var result = await _entries.ReplaceOneAsync(e => e.Id == id, entry);
        return result.IsAcknowledged && result.ModifiedCount > 0;
    }

    public async Task<bool> UpdateLocaleValueAsync(string key, string locale, string value)
    {
        var filter = Builders<DataBankEntryDocument>.Filter.And(
            Builders<DataBankEntryDocument>.Filter.Eq(e => e.Key, key),
            Builders<DataBankEntryDocument>.Filter.Eq(e => e.Context, (string?)null),
            Builders<DataBankEntryDocument>.Filter.ElemMatch(e => e.Values, v => v.Locale == locale));

        var update = Builders<DataBankEntryDocument>.Update.Set("Values.$.Value", value);

        var result = await _entries.UpdateOneAsync(filter, update);

        // If locale doesn't exist in Values array, add it
        if (result.MatchedCount == 0)
        {
            var addFilter = Builders<DataBankEntryDocument>.Filter.And(
                Builders<DataBankEntryDocument>.Filter.Eq(e => e.Key, key),
                Builders<DataBankEntryDocument>.Filter.Eq(e => e.Context, (string?)null));
            var addUpdate = Builders<DataBankEntryDocument>.Update.AddToSet(e => e.Values,
                new LocaleValueDocument { Locale = locale, Value = value });
            result = await _entries.UpdateOneAsync(addFilter, addUpdate);
        }

        return result.IsAcknowledged && result.MatchedCount > 0;
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

        return await _entries.CountDocumentsAsync(e => e.Values.Any(v => v.Locale == locale));
    }

    public async Task<long> GetUniqueKeyCountAsync()
    {
        // Each document is now one key, so count documents directly
        return await _entries.CountDocumentsAsync(_ => true);
    }

    public async Task<Dictionary<string, long>> GetEntryCountByLocaleAsync()
    {
        // Unwind the Values array to count per locale
        var pipeline = new[]
        {
            new BsonDocument("$unwind", "$Values"),
            new BsonDocument("$group", new BsonDocument
            {
                { "_id", "$Values.Locale" },
                { "count", new BsonDocument("$sum", 1) }
            })
        };
        var results = await _entries.Aggregate<BsonDocument>(pipeline).ToListAsync();
        return results.ToDictionary(r => r["_id"].AsString, r => r["count"].ToInt64());
    }

    public async Task<Dictionary<string, long>> GetEntryCountByFormatAsync()
    {
        // Unwind Sources dictionary keys to count per format
        var pipeline = new[]
        {
            new BsonDocument("$project", new BsonDocument
            {
                { "sourceFormats", new BsonDocument("$objectToArray", "$Sources") }
            }),
            new BsonDocument("$unwind", "$sourceFormats"),
            new BsonDocument("$group", new BsonDocument
            {
                { "_id", "$sourceFormats.v.Format" },
                { "count", new BsonDocument("$sum", 1) }
            })
        };
        var results = await _entries.Aggregate<BsonDocument>(pipeline).ToListAsync();
        return results.ToDictionary(r => r["_id"].AsString, r => r["count"].ToInt64());
    }

    public async Task<Dictionary<string, long>> GetTranslationStatusCountsAsync()
    {
        // Derive status from DoNotTranslate and IsTranslated
        var pipeline = new[]
        {
            new BsonDocument("$project", new BsonDocument
            {
                { "status", new BsonDocument("$switch", new BsonArray
                {
                    new BsonDocument("case", new BsonDocument("$eq", new BsonArray { "$Metadata.DoNotTranslate", true })),
                    new BsonDocument("then", "DoNotTranslate"),
                    new BsonDocument("case", new BsonDocument("$eq", new BsonArray { "$Metadata.IsTranslated", false })),
                    new BsonDocument("then", "Untranslated"),
                    new BsonDocument("case", new BsonDocument("$eq", new BsonArray { "$Metadata.IsTranslated", true })),
                    new BsonDocument("then", "Translated")
                })}
            }),
            new BsonDocument("$group", new BsonDocument
            {
                { "_id", "$status" },
                { "count", new BsonDocument("$sum", 1) }
            })
        };
        var results = await _entries.Aggregate<BsonDocument>(pipeline).ToListAsync();
        return results.ToDictionary(r => r["_id"].AsString, r => r["count"].ToInt64());
    }

    public async Task<Dictionary<string, Dictionary<string, long>>> GetTranslationStatusCountsByLocaleAsync()
    {
        // Unwind Values, then derive status per locale entry
        var pipeline = new[]
        {
            new BsonDocument("$unwind", "$Values"),
            new BsonDocument("$project", new BsonDocument
            {
                { "locale", "$Values.Locale" },
                { "hasValue", new BsonDocument("$gt", new BsonArray { new BsonDocument("$strLenCP", "$Values.Value"), 0 }) },
                { "doNotTranslate", "$Metadata.DoNotTranslate" }
            }),
            new BsonDocument("$project", new BsonDocument
            {
                { "locale", 1 },
                { "status", new BsonDocument("$switch", new BsonArray
                {
                    new BsonDocument("case", new BsonDocument("$eq", new BsonArray { "$doNotTranslate", true })),
                    new BsonDocument("then", "DoNotTranslate"),
                    new BsonDocument("case", new BsonDocument("$eq", new BsonArray { "$hasValue", false })),
                    new BsonDocument("then", "Untranslated"),
                    new BsonDocument("case", new BsonDocument("$eq", new BsonArray { "$hasValue", true })),
                    new BsonDocument("then", "Translated")
                })}
            }),
            new BsonDocument("$group", new BsonDocument
            {
                { "_id", new BsonDocument
                    {
                        { "locale", "$locale" },
                        { "status", "$status" }
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
