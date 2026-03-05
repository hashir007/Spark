using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace SparkService.Models
{
    public class EmailMessageWithFolders
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        [BsonElement("_email_message_id")]
        public string email_message_id { get; set; } = null!;

        [BsonRepresentation(BsonType.ObjectId)]
        [BsonElement("_email_message_folder_id")]
        public string email_message_folder_id { get; set; } = null!;

        [BsonRepresentation(BsonType.ObjectId)]
        [BsonElement("_reply_to_message_id")]
        public string? reply_to_message_id { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        [BsonElement("_userid")]
        public string user_id { get; set; } = null!;

        public bool is_read { get; set; }
        public bool is_starred { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime created_at { get; set; }

    }
}
