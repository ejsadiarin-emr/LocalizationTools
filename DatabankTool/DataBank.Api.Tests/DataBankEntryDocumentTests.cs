using DataBank.Api.Models;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace DataBank.Api.Tests;

public class DataBankEntryDocumentTests
{
    [Fact]
    public void DataBankEntryDocument_SerializesToBson_WithAllFields()
    {
        var entry = new DataBankEntryDocument
        {
            Id = "json::test.json::key1",
            Key = "key1",
            Values =
            [
                new LocaleValueDocument { Locale = "en", Value = "Test value" },
                new LocaleValueDocument { Locale = "zh-CN", Value = "测试值" }
            ],
            Sources = new Dictionary<string, SourceInfoDocument>
            {
                ["en"] = new SourceInfoDocument
                {
                    Format = "json",
                    File = "test.json",
                    Path = "test.json"
                },
                ["zh-CN"] = new SourceInfoDocument
                {
                    Format = "json",
                    File = "test.zh-CN.json",
                    Path = "test.zh-CN.json"
                }
            },
            Metadata = new EntryMetadataDocument
            {
                Comment = "test",
                IsTranslated = true,
                DoNotTranslate = false
            }
        };

        var doc = entry.ToBsonDocument();

        Assert.Equal("json::test.json::key1", doc["_id"].AsString);
        Assert.Equal("key1", doc["Key"].AsString);
        Assert.Equal(2, doc["Values"].AsBsonArray.Count);
        Assert.Equal("en", doc["Values"][0]["Locale"].AsString);
        Assert.Equal("Test value", doc["Values"][0]["Value"].AsString);
        Assert.Equal("zh-CN", doc["Values"][1]["Locale"].AsString);
        Assert.Equal("测试值", doc["Values"][1]["Value"].AsString);
        Assert.Equal("json", doc["Sources"]["en"]["Format"].AsString);
        Assert.Equal("test.json", doc["Sources"]["en"]["File"].AsString);
        Assert.True(doc["Metadata"]["IsTranslated"].AsBoolean);
    }

    [Fact]
    public void DataBankEntryDocument_RoundTripsThroughBson()
    {
        var original = new DataBankEntryDocument
        {
            Id = "round::trip::test",
            Key = "roundtrip",
            Values =
            [
                new LocaleValueDocument { Locale = "en", Value = "Round trip value" },
                new LocaleValueDocument { Locale = "zh", Value = "往返值" }
            ],
            Sources = new Dictionary<string, SourceInfoDocument>
            {
                ["en"] = new SourceInfoDocument
                {
                    Format = "rc",
                    File = "test.rc",
                    Path = "test.rc"
                }
            },
            Metadata = new EntryMetadataDocument
            {
                IsTranslated = false,
                DoNotTranslate = true
            }
        };

        var bson = original.ToBsonDocument();
        var deserialized = BsonSerializer.Deserialize<DataBankEntryDocument>(bson);

        Assert.Equal(original.Id, deserialized.Id);
        Assert.Equal(original.Key, deserialized.Key);
        Assert.Equal(2, deserialized.Values.Count);
        Assert.Equal("en", deserialized.Values[0].Locale);
        Assert.Equal("Round trip value", deserialized.Values[0].Value);
        Assert.Equal("rc", deserialized.Sources["en"].Format);
        Assert.Equal(original.Metadata.IsTranslated, deserialized.Metadata.IsTranslated);
        Assert.Equal(original.Metadata.DoNotTranslate, deserialized.Metadata.DoNotTranslate);
    }

    [Fact]
    public void DataBankEntryDocument_MatchesJsonFormat()
    {
        var entry = new DataBankEntryDocument
        {
            Id = "json::translate.en.json::TestKey",
            Key = "TestKey",
            Values =
            [
                new LocaleValueDocument { Locale = "en", Value = "Test value" }
            ],
            Sources = new Dictionary<string, SourceInfoDocument>
            {
                ["en"] = new SourceInfoDocument
                {
                    Format = "json",
                    File = "translate.en.json",
                    Path = "translate.en.json"
                }
            },
            Metadata = new EntryMetadataDocument
            {
                FormatSpecifiers = [],
                DoNotTranslate = false,
                IsTranslated = false
            }
        };

        var json = entry.ToJson(new MongoDB.Bson.IO.JsonWriterSettings { Indent = true });

        Assert.Contains("\"Key\" : \"TestKey\"", json);
        Assert.Contains("\"IsTranslated\" : false", json);
    }
}
