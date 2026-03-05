using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace SparkService.Models
{
    public class File
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }
        public string originalName { get; set; } = null!;
        public string name { get; set; } = null!;
        public string type { get; set; } = null!;

        public string path_original { get; set; } = null!;
        public string query_original { get; set; } = null!;

        public string path_480x320 { get; set; } = null!;
        public string query_480x320 { get; set; } = null!;

        public string path_300x300 { get; set; } = null!;
        public string query_300x300 { get; set; } = null!;

        public string path_100x100 { get; set; } = null!;
        public string query_100x100 { get; set; } = null!;

        public string path_32x32 { get; set; } = null!;
        public string query_32x32 { get; set; } = null!;

        public string path_16x16 { get; set; } = null!;
        public string query_16x16 { get; set; } = null!;

        public double size { get; set; } 

        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime created_at { get; set; }



    }
}
