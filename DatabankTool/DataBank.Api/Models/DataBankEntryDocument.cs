using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace DataBank.Api.Models;

public class DataBankEntryDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("Key")]
    public string Key { get; set; } = string.Empty;

    [BsonElement("Values")]
    public List<LocaleValueDocument> Values { get; set; } = [];

    [BsonElement("Sources")]
    public Dictionary<string, SourceInfoDocument> Sources { get; set; } = [];

    [BsonElement("Metadata")]
    public EntryMetadataDocument Metadata { get; set; } = new();
}

public class LocaleValueDocument
{
    [BsonElement("Locale")]
    public string Locale { get; set; } = string.Empty;

    [BsonElement("Value")]
    public string Value { get; set; } = string.Empty;
}

public class SourceInfoDocument
{
    [BsonElement("Format")]
    public string Format { get; set; } = string.Empty;

    [BsonElement("File")]
    public string File { get; set; } = string.Empty;

    [BsonElement("Path")]
    public string Path { get; set; } = string.Empty;
}

public class EntryMetadataDocument
{
    [BsonElement("Comment")]
    public string? Comment { get; set; }

    [BsonElement("FormatSpecifiers")]
    public List<string> FormatSpecifiers { get; set; } = [];

    [BsonElement("DoNotTranslate")]
    public bool DoNotTranslate { get; set; }

    [BsonElement("IsTranslated")]
    public bool IsTranslated { get; set; }
}
