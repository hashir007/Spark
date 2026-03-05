using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SparkService.Models
{
    public class Photos
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        [BsonElement("_fileId")]
        public string fileId { get; set; } = null!;

        public string? passCode { get; set; } 

        [BsonRepresentation(BsonType.ObjectId)]
        [BsonElement("_userid")]
        public string? userId { get; set; }
        public bool is_private { get; set; }
        public bool is_adult { get;set; }
        public bool is_featured { get; set;}
        public bool is_members_only { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime created_at { get; set; }
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime updated_at { get; set; }
    }
}
