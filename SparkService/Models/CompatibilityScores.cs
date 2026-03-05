using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace SparkService.Models
{
    public class CompatibilityScores
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        [BsonElement("_user_id")]
        public string user_id { get; set; } = null!;

        [BsonRepresentation(BsonType.ObjectId)]
        [BsonElement("_other_user_id")]
        public string other_user_id { get; set; } = null!;

        public int score { get; set; } 
    }
}
