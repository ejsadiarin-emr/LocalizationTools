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

    [BsonElement("Value")]
    public string Value { get; set; } = string.Empty;

    [BsonElement("Locale")]
    public string Locale { get; set; } = string.Empty;

    [BsonElement("Source")]
    public SourceInfoDocument Source { get; set; } = new();

    [BsonElement("Metadata")]
    public EntryMetadataDocument Metadata { get; set; } = new();
}

public class SourceInfoDocument
{
    [BsonElement("Format")]
    public string Format { get; set; } = string.Empty;

    [BsonElement("File")]
    public string File { get; set; } = string.Empty;

    [BsonElement("Path")]
    public string Path { get; set; } = string.Empty;

    [BsonElement("Encoding")]
    public string? Encoding { get; set; }
}

public class EntryMetadataDocument
{
    [BsonElement("Comment")]
    public string? Comment { get; set; }

    [BsonElement("RcId")]
    public int? RcId { get; set; }

    [BsonElement("RcDefine")]
    public string? RcDefine { get; set; }

    [BsonElement("IsBehavioral")]
    public bool IsBehavioral { get; set; }

    [BsonElement("FormatSpecifiers")]
    public List<string> FormatSpecifiers { get; set; } = [];

    [BsonElement("DoNotTranslate")]
    public bool DoNotTranslate { get; set; }

    [BsonElement("IsTranslated")]
    public bool IsTranslated { get; set; }

    [BsonElement("TranslationStatus")]
    public string TranslationStatus { get; set; } = "Untranslated";
}
