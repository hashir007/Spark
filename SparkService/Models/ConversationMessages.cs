using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;


namespace SparkService.Models
{
    public class ConversationMessages
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        public string Text { get; set; } = null!;

        [BsonRepresentation(BsonType.ObjectId)]
        [BsonElement("_reply_to_message_id")]
        public string? reply_to_message_id { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        [BsonElement("_conversationId")]
        public string ConversationId { get; set; } = null!;

        public int status { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        [BsonElement("_created_by")]
        public string created_by { get; set; } = null!;

        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime created_at { get; set; }

    }
}
