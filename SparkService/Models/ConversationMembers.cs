using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;


namespace SparkService.Models
{
    public class ConversationMembers
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }      

        [BsonRepresentation(BsonType.ObjectId)]
        [BsonElement("_userid")]
        public string UserId { get; set; } = null!;

        [BsonRepresentation(BsonType.ObjectId)]
        [BsonElement("_conversationId")]
        public string ConversationId { get; set; } = null!;

        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime created_at { get; set; }
    }
}
