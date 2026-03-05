using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace SparkService.Models
{
    public class EmailMessageAttachments
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }
      
        public string link { get; set; } = null!;

        [BsonRepresentation(BsonType.ObjectId)]
        [BsonElement("_email_message_id")]
        public string email_message_id { get; set; } = null!;
    }
}
