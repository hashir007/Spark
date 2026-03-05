using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using static MongoDB.Bson.Serialization.Serializers.SerializerHelper;

namespace SparkService.Models
{
    public enum ConversationType { Chat,Direct }
    public class Conversations
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }           
        public string Subject { get; set; } = null!;

        public string Type { get; set; } = null!;

        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime created_at { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        [BsonElement("_created_by")]
        public string created_by { get; set; } = null!;
    }
}
