using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SparkService.Models
{
    public class InterestCategories
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        public string name { get; set; } = null!;

    }
}
