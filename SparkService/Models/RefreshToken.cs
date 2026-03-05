using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SparkService.Models
{
    public class RefreshToken
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        public string token { get; set; } = null!;

        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime expires { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime created_at { get; set; }

        public string created_by_ip { get; set; } = null!;

        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime? revoked { get; set; }

        public string revoked_by_ip { get; set; } = null!;

        public string replaced_by_token { get; set; } = null!;

        public string? reason_revoked { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        [BsonElement("_userid")]
        public string? UserId { get; set; }

        [BsonRepresentation(BsonType.Boolean)]
        public bool IsExpired => DateTime.UtcNow >= expires;

        [BsonRepresentation(BsonType.Boolean)]
        public bool IsRevoked => revoked != null;

        [BsonRepresentation(BsonType.Boolean)]
        public bool IsActive => !IsRevoked && !IsExpired;
    }
}
