using SparkService.Models;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace SparkService.ViewModels
{
    public class ConversationViewModel
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? id { get; set; }
        public int? unreadCount { get; set; } = null!;
        public bool unread { get; set; }
        public ConversationMessageViewModel? lastUnreadMessage { get; set; }
        public string subject { get; set; } = null!;
        public string type { get; set; } = null!;
        public DateTime created_at { get; set; }
        public List<ConversationMemberViewModel> members { get; set; } = null!;
        [BsonRepresentation(BsonType.ObjectId)]
        public string? created_by { get; set; }
        public UserViewModelV4? created_by_user { get; set; }

        public ConversationViewModel()
        {
            this.members = new List<ConversationMemberViewModel>();

        }
    }
}
