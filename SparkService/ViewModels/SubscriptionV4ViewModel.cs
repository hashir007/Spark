using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SparkService.ViewModels
{
    public class SubscriptionV4ViewModel
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        public SubscriptionPlansV4ViewModel Plan { get; set; } = null!;

        public string status { get; set; } = null!;
        public string source { get; set; } = null!;

        public string paypal_subscriptionId { get; set; } = null!;
        public string authorizenet_subscriptionId { get; set; } = null!;

        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime start_date { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime? end_date { get; set; }
    }
}
