using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SparkService.ViewModels
{
    public class ConversationMessageEditedReceiptViewModel
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public string userId { get; set; } = null!;

        public UserViewModelV4? user { get; set; }
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime created_at { get; set; }
    }
}
