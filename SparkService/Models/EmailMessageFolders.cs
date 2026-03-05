using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace SparkService.Models
{
    public class EmailMessageFolders
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        public string name { get; set; } = null!;    

        public bool is_system { get; set; } = false;

        [BsonRepresentation(BsonType.ObjectId)]
        [BsonElement("_created_by")]
        public string? created_by { get; set; } 


    }
}
