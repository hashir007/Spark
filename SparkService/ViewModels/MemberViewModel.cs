using SparkService.Models;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace SparkService.ViewModels
{
    public class MemberViewModel
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? id { get; set; }
        public string username { get; set; } = null!;       
        public bool is_active { get; set; }
        public bool? isLike { get; set; }
        public long total_like_count {  get; set; }
        public long total_dislike_count { get; set; }
        public bool isFavorite { get; set; }
        public bool iskissed { get; set; }    
        public bool isFriend { get; set; }
        public long total_kiss_count { get; set; }
        public bool isBlocked { get; set; }
        public string timezone { get; set; } = null!;
        public string language { get; set; } = null!;
        public long galleyPhotoCount { get; set; }
        public DateTime lastLogin { get; set; }
        public MemberProfileViewModel profile { get; set; } = null!;
        public SubscriptionV3ViewModel? Subscription { get; set; } = null!;
    }
}
