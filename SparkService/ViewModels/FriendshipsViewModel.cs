using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace SparkService.ViewModels
{
    public class FriendshipsViewModel
    {       
         
        public string friend_id { get; set; } = null!;
        public UserViewModelV2 friend { get; set; } = null!;
      
    }
}
