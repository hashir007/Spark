using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace SparkService.Models
{
    public enum EmailStatus { Pending, Sent }
    public class EmailMessage
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }
        public string content { get; set; } = null!;

        public string subject { get; set; } = null!;

        [BsonRepresentation(BsonType.ObjectId)]
        [BsonElement("_reply_to_message_id")]
        public string? reply_to_message_id { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        [BsonElement("_created_by")]
        public string created_by { get; set; } = null!;

        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime created_at { get; set; }
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime updated_at { get; set; }
        public string status { get; set; } = null!;
    }
}
