using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SparkService.Models
{
    public class ViewsProfile
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }      

        [BsonRepresentation(BsonType.ObjectId)]
        [BsonElement("_userid")]
        public string? userId { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        [BsonElement("_profileId")]
        public string? profileId { get; set; }

        public string ip_address { get; set; } = null!;

        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime created_at { get; set; }
    }
}
