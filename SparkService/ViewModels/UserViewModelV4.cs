using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SparkService.ViewModels
{
    public class UserViewModelV4
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? id { get; set; }
        public string username { get; set; } = null!;
        public bool is_active { get; set; }
        public string timezone { get; set; } = null!;
        public string language { get; set; } = null!;
        public DateTime last_login { get; set; }
        public ProfileViewModelV4 profile { get; set; }

        public SubscriptionV3ViewModel? Subscription { get; set; } = null!;
    }
}
