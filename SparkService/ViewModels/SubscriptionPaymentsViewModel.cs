using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SparkService.Models;

namespace SparkService.ViewModels
{
    public class SubscriptionPaymentsViewModel
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        [BsonElement("_subscriptionId")]
        public string subscriptionId { get; set; } = null!;

        [BsonRepresentation(BsonType.ObjectId)]
        [BsonElement("_userId")]
        public string userId { get; set; } = null!;

        public string api_response { get; set; } = null!;

        public string event_type { get; set; } = null!;

        public decimal amount { get; set; }

        public string status { get; set; } = null!;

        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime created_at { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime updated_at { get; set; }


        public SubscriptionPaymentsViewModel ToSubscriptionPaymentsViewModel(SubscriptionPayments model)
        {
            return new SubscriptionPaymentsViewModel()
            {
                Id = model.Id,
                api_response = model.api_response,
                amount = model.amount,
                created_at = model.created_at,
                event_type = model.event_type,
                updated_at = model.updated_at,
                status = model.status,
                subscriptionId = model.subscriptionId,
                userId = model.userId
            };
        }
    }
}
