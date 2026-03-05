using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace SparkService.Models
{
    public class Traits
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        public string Description { get; set; } = null!;
    }
}
