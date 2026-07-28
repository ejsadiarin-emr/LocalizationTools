using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace DataBank.Api.Models;

public class DataBankMetadataDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("Version")]
    public int Version { get; set; }

    [BsonElement("Generated")]
    public string Generated { get; set; } = string.Empty;

    [BsonElement("EntryCount")]
    public int EntryCount { get; set; }
}
