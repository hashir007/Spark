using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace SparkService.ViewModels
{
    public class ConversationFileViewModel
    {
        public string? id { get; set; }
        public string link { get; set; } = null!;
    }
}
