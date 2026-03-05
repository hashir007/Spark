using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;


namespace SparkService.Models
{
    public class SubscriptionsHistories
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        [BsonElement("_subscriptionPlansId")]
        public string subscriptionPlansId { get; set; } = null!;

        [BsonRepresentation(BsonType.ObjectId)]
        [BsonElement("_subscriptionId")]
        public string subscriptionId { get; set; } = null!;

        [BsonRepresentation(BsonType.ObjectId)]
        [BsonElement("_userId")]
        public string userId { get; set; } = null!;

        public string api_response { get; set; } = null!;

        public string event_type { get; set; } = null!;

        public string summary { get; set; } = null!;

        public string source { get; set; } = null!;

        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime start_date { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime? end_date { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime created_at { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime updated_at { get; set; }
    }
}
