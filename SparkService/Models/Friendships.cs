using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SparkService.Models
{
    public enum FriendshipsStatus { Accepted, Pending, Blocked }
    public class Friendships
    {

        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        [BsonElement("_user_id")]
        public string user_id { get; set; } = null!;

        [BsonRepresentation(BsonType.ObjectId)]
        [BsonElement("_friend_id")]
        public string friend_id { get; set; } = null!;

        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime created_at { get; set; }

        public string status { get; set; } = null!;
    }
}
