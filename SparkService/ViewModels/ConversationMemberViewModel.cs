using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace SparkService.ViewModels
{
    public class ConversationMemberViewModel
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? id { get; set; }
     
        [BsonRepresentation(BsonType.ObjectId)]
        public string userId { get; set; } = null!;

        public UserViewModelV4? user { get; set; }
      
        [BsonRepresentation(BsonType.ObjectId)]
        public string conversationId { get; set; } = null!;
        public DateTime created_at { get; set; }
    }
}
