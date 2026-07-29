using DataBank.Api.Models;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace DataBank.Api.Tests;

public class EntryMetadataDocumentTests
{
    [Fact]
    public void EntryMetadataDocument_HasIsTranslatedField()
    {
        var metadata = new EntryMetadataDocument
        {
            IsTranslated = true
        };

        Assert.True(metadata.IsTranslated);
    }

    [Fact]
    public void EntryMetadataDocument_HasTranslationStatusField()
    {
        var metadata = new EntryMetadataDocument
        {
            TranslationStatus = "Translated"
        };

        Assert.Equal("Translated", metadata.TranslationStatus);
    }

    [Fact]
    public void EntryMetadataDocument_DefaultTranslationStatus_IsUntranslated()
    {
        var metadata = new EntryMetadataDocument();

        Assert.Equal("Untranslated", metadata.TranslationStatus);
    }

    [Fact]
    public void EntryMetadataDocument_DefaultIsTranslated_IsFalse()
    {
        var metadata = new EntryMetadataDocument();

        Assert.False(metadata.IsTranslated);
    }

    [Fact]
    public void EntryMetadataDocument_SerializesToBson_WithAllFields()
    {
        var metadata = new EntryMetadataDocument
        {
            Comment = "test comment",
            RcId = 123,
            RcDefine = "DEFINE",
            IsBehavioral = true,
            FormatSpecifiers = ["%d", "%s"],
            DoNotTranslate = true,
            IsTranslated = true,
            TranslationStatus = "Translated"
        };

        var doc = metadata.ToBsonDocument();

        Assert.Equal("test comment", doc["Comment"].AsString);
        Assert.Equal(123, doc["RcId"].AsInt32);
        Assert.Equal("DEFINE", doc["RcDefine"].AsString);
        Assert.True(doc["IsBehavioral"].AsBoolean);
        Assert.Equal(2, doc["FormatSpecifiers"].AsBsonArray.Count);
        Assert.True(doc["DoNotTranslate"].AsBoolean);
        Assert.True(doc["IsTranslated"].AsBoolean);
        Assert.Equal("Translated", doc["TranslationStatus"].AsString);
    }

    [Fact]
    public void EntryMetadataDocument_RoundTripsThroughBson()
    {
        var original = new EntryMetadataDocument
        {
            Comment = "round trip test",
            RcId = 456,
            IsTranslated = true,
            TranslationStatus = "NeedsReview"
        };

        var bson = original.ToBsonDocument();
        var deserialized = BsonSerializer.Deserialize<EntryMetadataDocument>(bson);

        Assert.Equal(original.Comment, deserialized.Comment);
        Assert.Equal(original.RcId, deserialized.RcId);
        Assert.Equal(original.IsTranslated, deserialized.IsTranslated);
        Assert.Equal(original.TranslationStatus, deserialized.TranslationStatus);
    }
}
