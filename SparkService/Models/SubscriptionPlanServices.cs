using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SparkService.Models
{
    public class SubscriptionPlanServices
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        [BsonElement("_subscriptionServicesId")]
        public string subscriptionServicesId { get; set; } = null!;

        [BsonRepresentation(BsonType.ObjectId)]
        [BsonElement("_subscriptionPlansId")]
        public string subscriptionPlansId { get; set; } = null!;

    }
}
