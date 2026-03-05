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
    public class SubscriptionPlanServicesViewModel
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? id { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        [BsonElement("_subscriptionServicesId")]
        public string subscriptionServicesId { get; set; } = null!;

        [BsonRepresentation(BsonType.ObjectId)]
        [BsonElement("_subscriptionPlansId")]
        public string subscriptionPlansId { get; set; } = null!;




        public SubscriptionPlanServicesViewModel ToPlanServicesViewModel(SubscriptionPlanServices model)
        {
            return new SubscriptionPlanServicesViewModel
            {
                id = model.Id,
                subscriptionPlansId = model.subscriptionPlansId,
                subscriptionServicesId = model.subscriptionServicesId
            };
        }
    }
}
