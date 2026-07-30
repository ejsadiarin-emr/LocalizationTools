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
    public void EntryMetadataDocument_DefaultDoNotTranslate_IsFalse()
    {
        var metadata = new EntryMetadataDocument();

        Assert.False(metadata.DoNotTranslate);
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
            FormatSpecifiers = ["%d", "%s"],
            DoNotTranslate = true,
            IsTranslated = true
        };

        var doc = metadata.ToBsonDocument();

        Assert.Equal("test comment", doc["Comment"].AsString);
        Assert.Equal(2, doc["FormatSpecifiers"].AsBsonArray.Count);
        Assert.True(doc["DoNotTranslate"].AsBoolean);
        Assert.True(doc["IsTranslated"].AsBoolean);
    }

    [Fact]
    public void EntryMetadataDocument_RoundTripsThroughBson()
    {
        var original = new EntryMetadataDocument
        {
            Comment = "round trip test",
            IsTranslated = true,
            DoNotTranslate = false,
            FormatSpecifiers = ["%s"]
        };

        var bson = original.ToBsonDocument();
        var deserialized = BsonSerializer.Deserialize<EntryMetadataDocument>(bson);

        Assert.Equal(original.Comment, deserialized.Comment);
        Assert.Equal(original.IsTranslated, deserialized.IsTranslated);
        Assert.Equal(original.DoNotTranslate, deserialized.DoNotTranslate);
        Assert.Equal(original.FormatSpecifiers, deserialized.FormatSpecifiers);
    }
}
