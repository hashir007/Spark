using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace SparkService.Models
{
    public class ConversationFiles
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        [BsonElement("_conversationId")]
        public string ConversationId { get; set; } = null!;

        [BsonRepresentation(BsonType.ObjectId)]
        [BsonElement("_messageId")]
        public string MessageId { get; set; } = null!;

        public string Link { get; set; } = null!;
    }
}
