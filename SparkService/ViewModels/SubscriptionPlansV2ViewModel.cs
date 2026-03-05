using SparkService.Models;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SparkService.ViewModels
{
    public class SubscriptionPlansV2ViewModel
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? id { get; set; }
        public string name { get; set; } = null!;
        public string description { get; set; } = null!;
        public string descriptionHTML { get; set; } = null!;
        public decimal price { get; set; }
        public string status { get; set; } = null!;
        public string type { get; set; } = null!;
        public string colour { get; set; } = null!;
        public long storage { get; set; }

        public int order { get; set; }

        public string paypal_plan_id { get; set; } = null!;

        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime created_at { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime updated_at { get; set; }

        public List<SubscriptionServices> SubscriptionServices { get; set; } = null!;

        public SubscriptionPlansV2ViewModel ToSubscriptionPlansViewModel(SubscriptionPlans model)
        {

            return new SubscriptionPlansV2ViewModel()
            {
                id = model.Id,
                name = model.name,
                description = model.description,
                descriptionHTML = model.descriptionHTML,
                price = model.price,
                status = model.status,
                type = model.type,
                colour = model.colour,
                storage = model.storage,
                order = model.order,
                paypal_plan_id = model.paypal_plan_id,
                created_at = model.created_at,
                updated_at = model.updated_at
            };
        }
    }
}
