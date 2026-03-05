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
    public class SubscriptionServicesViewModel
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }
        public string name { get; set; } = null!;

        public string description { get; set; } = null!;

        public SubscriptionServicesViewModel ToSubscriptionServicesViewModel(SubscriptionServices model)
        {
            return new SubscriptionServicesViewModel()
            {
                Id = model.Id,
                name = model.name,
                description = model.description,
            };
        }
    }
}
