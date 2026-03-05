using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using SparkService.Models;

namespace SparkService.ViewModels
{
    public class ConversationMessageViewModel
    {
        public List<ConversationFileViewModel> files { get; set; } = null!;
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? id { get; set; }

        public string text { get; set; } = null!;

        [BsonRepresentation(BsonType.ObjectId)]       
        public string? reply_to_message_id { get; set; }
        public ConversationMessageViewModel? reply_to_message { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        public string conversationId { get; set; } = null!;

        public ConversationViewModel conversations { get; set; } = null!;

        [BsonRepresentation(BsonType.ObjectId)]
        public string created_by { get; set; } = null!;
        public UserViewModelV4? created_by_user { get; set; }

        public List<ConversationMessageReadReceiptViewModel> readReceipt { get; set; }

        public ConversationMessageEditedReceiptViewModel? editReceipt { get; set; }

        public ConversationMessageDeletedReceipt? deleteReceipt { get; set; }

        public DateTime created_at { get; set; }


        public ConversationMessageViewModel()
        {
            files = new List<ConversationFileViewModel>();
        }
    }
}
