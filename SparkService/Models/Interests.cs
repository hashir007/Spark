using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace SparkService.Models
{
    public class Interests
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }
        public string interest_description { get; set; } = null!;
        
        [BsonRepresentation(BsonType.ObjectId)]
        [BsonElement("_category_id")]
        public string category_id { get; set; } = null!;

        public int popularity { get; set; }
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime created_at { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        [BsonElement("_created_by")]
        public string? created_by { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime modified_at { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        [BsonElement("_modified_by")]
        public string? modified_by { get; set; }

        public bool is_active { get; set; }
        public bool is_featured { get; set; }

    }
}
