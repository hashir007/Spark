using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace SparkService.Models
{
    public class UserTraits
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        [BsonElement("_user_id")]
        public string user_id { get; set; } = null!;

        [BsonRepresentation(BsonType.ObjectId)]
        [BsonElement("_trait_id")]
        public string trait_id { get; set; } = null!;

        public int trait_value { get; set; }
    }
}
