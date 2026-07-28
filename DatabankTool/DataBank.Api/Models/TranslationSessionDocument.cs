using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace DataBank.Api.Models;

public class TranslationSessionDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public ObjectId Id { get; set; }

    [BsonElement("SessionName")]
    public string SessionName { get; set; } = string.Empty;

    [BsonElement("SourceLocale")]
    public string SourceLocale { get; set; } = string.Empty;

    [BsonElement("TargetLocale")]
    public string TargetLocale { get; set; } = string.Empty;

    [BsonElement("Status")]
    public string Status { get; set; } = TranslationSessionStatus.Pending;

    [BsonElement("CreatedAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("UpdatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("EntryIds")]
    public List<string> EntryIds { get; set; } = [];
}
