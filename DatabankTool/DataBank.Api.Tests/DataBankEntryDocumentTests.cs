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
            Value = "Test value",
            Locale = "en",
            Source = new SourceInfoDocument
            {
                Format = "json",
                File = "test.json",
                Path = "test.json",
                Encoding = "utf-8"
            },
            Metadata = new EntryMetadataDocument
            {
                Comment = "test",
                IsTranslated = true,
                TranslationStatus = "Translated"
            }
        };

        var doc = entry.ToBsonDocument();

        Assert.Equal("json::test.json::key1", doc["_id"].AsString);
        Assert.Equal("key1", doc["Key"].AsString);
        Assert.Equal("Test value", doc["Value"].AsString);
        Assert.Equal("en", doc["Locale"].AsString);
        Assert.Equal("json", doc["Source"]["Format"].AsString);
        Assert.Equal("test.json", doc["Source"]["File"].AsString);
        Assert.Equal("utf-8", doc["Source"]["Encoding"].AsString);
        Assert.True(doc["Metadata"]["IsTranslated"].AsBoolean);
        Assert.Equal("Translated", doc["Metadata"]["TranslationStatus"].AsString);
    }

    [Fact]
    public void DataBankEntryDocument_RoundTripsThroughBson()
    {
        var original = new DataBankEntryDocument
        {
            Id = "round::trip::test",
            Key = "roundtrip",
            Value = "Round trip value",
            Locale = "zh",
            Source = new SourceInfoDocument
            {
                Format = "rc",
                File = "test.rc",
                Path = "test.rc"
            },
            Metadata = new EntryMetadataDocument
            {
                IsTranslated = false,
                TranslationStatus = "Untranslated",
                DoNotTranslate = true
            }
        };

        var bson = original.ToBsonDocument();
        var deserialized = BsonSerializer.Deserialize<DataBankEntryDocument>(bson);

        Assert.Equal(original.Id, deserialized.Id);
        Assert.Equal(original.Key, deserialized.Key);
        Assert.Equal(original.Value, deserialized.Value);
        Assert.Equal(original.Locale, deserialized.Locale);
        Assert.Equal(original.Source.Format, deserialized.Source.Format);
        Assert.Equal(original.Metadata.IsTranslated, deserialized.Metadata.IsTranslated);
        Assert.Equal(original.Metadata.TranslationStatus, deserialized.Metadata.TranslationStatus);
        Assert.Equal(original.Metadata.DoNotTranslate, deserialized.Metadata.DoNotTranslate);
    }

    [Fact]
    public void DataBankEntryDocument_MatchesJsonFormat()
    {
        var entry = new DataBankEntryDocument
        {
            Id = "json::translate.en.json::TestKey",
            Key = "TestKey",
            Value = "Test value",
            Locale = "en",
            Source = new SourceInfoDocument
            {
                Format = "json",
                File = "translate.en.json",
                Path = "translate.en.json"
            },
            Metadata = new EntryMetadataDocument
            {
                IsBehavioral = false,
                FormatSpecifiers = [],
                DoNotTranslate = false,
                IsTranslated = false,
                TranslationStatus = "Untranslated"
            }
        };

        var json = entry.ToJson(new MongoDB.Bson.IO.JsonWriterSettings { Indent = true });

        Assert.Contains("\"Key\" : \"TestKey\"", json);
        Assert.Contains("\"Value\" : \"Test value\"", json);
        Assert.Contains("\"Locale\" : \"en\"", json);
        Assert.Contains("\"IsTranslated\" : false", json);
        Assert.Contains("\"TranslationStatus\" : \"Untranslated\"", json);
    }
}
