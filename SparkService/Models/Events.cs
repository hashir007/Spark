using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SparkService.Models
{
    public enum EventType { social, networking, sports }
    public enum EventStatus { upcoming, ongoing, completed }
    public class Events
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }
        public string name { get; set; } = null!;
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime start_date { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        [BsonElement("_venueId")]
        public string venueId { get; set; } = null!;
        public string description { get; set; } = null!;
        public string type { get; set; } = null!;

        [BsonRepresentation(BsonType.ObjectId)]
        [BsonElement("_organizerId")]
        public string organizerId { get; set; } = null!;
        public int capacity { get; set; } 
        public int attendees { get; set; }
        public string status { get; set; } = null!;
        public string created_by { get; set; } = null!;
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime created_at { get; set; }
        public string modified_by { get; set; } = null!;
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime updated_at { get; set; }
    }
}
