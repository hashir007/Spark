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
    public class SubscriptionsViewModel
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        [BsonElement("_subscriptionPlansId")]
        public string subscriptionPlansId { get; set; } = null!;

        public SubscriptionPlansViewModel Plan { get; set; } = null!;

        [BsonRepresentation(BsonType.ObjectId)]
        [BsonElement("_userId")]
        public string userId { get; set; } = null!;

        public string status { get; set; } = null!;

        public string source { get; set; } = null!;

        public string? paypal_subscriptionId { get; set; }
        public string? authorizenet_subscriptionId { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime start_date { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime? end_date { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime created_at { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime updated_at { get; set; }


        public SubscriptionsViewModel ToSubscriptionsViewModel(Subscriptions model)
        {
            return new SubscriptionsViewModel()
            {
                Id = model.Id,
                subscriptionPlansId = model.subscriptionPlansId,
                userId = model.userId,
                status = model.status,
                created_at = model.created_at,
                updated_at = model.updated_at,
                end_date = model.end_date,
                start_date = model.start_date
            };
        }
    }

}
