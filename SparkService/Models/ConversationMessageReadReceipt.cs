using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SparkService.Models
{
    public class ConversationMessageReadReceipt
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        [BsonElement("_userid")]
        public string userId { get; set; } = null!;

        [BsonRepresentation(BsonType.ObjectId)]
        [BsonElement("_conversationId")]
        public string conversationId { get; set; } = null!;

        [BsonRepresentation(BsonType.ObjectId)]
        [BsonElement("_messageId")]
        public string messageId { get; set; } = null!;

        public bool isRead { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime created_at { get; set; }
    }
}
