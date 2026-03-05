using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SparkService.ViewModels
{
    public class BlockedListViewModel
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        [BsonElement("_member_id")]
        public string member_id { get; set; } = null!;

        public UserViewModelV3? Member { get; set; }


        [BsonRepresentation(BsonType.ObjectId)]
        [BsonElement("_blocked_by")]
        public string blocked_by { get; set; } = null!;
        public UserViewModelV4? User { get; set; }


        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime created_at { get; set; }
    }
}
