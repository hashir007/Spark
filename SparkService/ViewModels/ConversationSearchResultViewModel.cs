using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SparkService.ViewModels
{
    public class ConversationSearchResultViewModel
    {
        public string? id { get; set; }
        public string subject { get; set; } = null!;
        public string type { get; set; } = null!;
        public ConversationMessageViewModel? message { get; set; }
        public DateTime created_at { get; set; }
        public string created_by { get; set; } = null!;
        public UserViewModelV4? created_by_user { get; set; }
        public List<ConversationMemberViewModel> members { get; set; } = null!;
    }
}
