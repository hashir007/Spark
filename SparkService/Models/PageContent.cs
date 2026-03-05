using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace SparkService.Models
{
    public enum PageStatus {Published,Draft,Archived }
    public enum PageVisibility {Public,Private,Restricted }
    public class PageContent
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }
        public string? content { get; set; }
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime created_at { get; set; }
        [BsonRepresentation(BsonType.ObjectId)]
        [BsonElement("_created_by")]
        public string created_by { get; set; } = null!;
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime modified_at { get; set; }
        [BsonRepresentation(BsonType.ObjectId)]
        [BsonElement("_modified_by")]
        public string modified_by { get; set; } = null!;
        public string page_title { get; set; } = null!;
        public string page_slug { get; set; } = null!;
        public string page_description { get; set; } = null!;
        public string page_keywords { get; set; } = null!;
        public PageStatus page_status { get; set; }
        public PageVisibility page_visibility { get; set; }
        public int page_order { get; set; }
        public bool is_locked { get; set; }
    }
}
