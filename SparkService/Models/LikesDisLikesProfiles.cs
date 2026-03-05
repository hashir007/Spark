using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace SparkService.Models
{
    public class LikesDisLikesProfiles
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        [BsonElement("_user_id")]
        public string user_id { get; set; } = null!;

        [BsonRepresentation(BsonType.ObjectId)]
        [BsonElement("_profile_id")]
        public string profile_id { get; set; } = null!;

        public bool isLikes { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime created_at { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime updated_at { get; set; }

    }
}
