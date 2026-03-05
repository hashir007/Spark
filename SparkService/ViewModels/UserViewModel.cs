using SparkService.Models;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace SparkService.ViewModels
{
    public class UserViewModel
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? id { get; set; }
        public string username { get; set; } = null!;             
        public string email_address { get; set; } = null!;              
        public bool is_premium { get; set; }
        public bool is_email_verified { get; set; }
        public bool is_photo_uploaded { get; set; }
        public bool is_active { get; set; }       
        public string timezone { get; set; } = null!;
        public string language { get; set; } = null!;
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime created_at { get; set; }
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime updated_at { get; set; }
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime last_login { get; set; }
        public List<Roles> roles { get; set; } = null!;               
        public ProfileViewModel profile { get; set; } = null!;
        public SubscriptionsViewModel? Subscription { get; set; } = null!;
    }
}
