using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MigrateMongo;

/// <summary>
/// Represents a document in the changelog collection tracking applied migrations.
/// </summary>
internal sealed class ChangelogEntry
{
    [BsonId]
    public ObjectId Id { get; set; }

    [BsonElement("fileName")]
    public required string FileName { get; set; }

    [BsonElement("appliedAt")]
    public required DateTime AppliedAt { get; set; }

    [BsonElement("fileHash")]
    [BsonIgnoreIfNull]
    public string? FileHash { get; set; }
}
