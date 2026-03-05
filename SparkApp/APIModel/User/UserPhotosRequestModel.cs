using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using SparkService.Models;

namespace SparkApp.APIModel.User
{
    public class UserPhotosRequestModel
    {
        public List<UserPhotosModel> Photos { get; set; }

        public UserPhotosRequestModel()
        {
            Photos = new List<UserPhotosModel>();
        }
    }
    public class UserPhotosModel
    {
        public string fileId { get; set; } = null!;        
        public bool is_private { get; set; }
        public bool is_adult { get; set; }
        public bool is_featured { get; set; }
        public bool is_members_only { get; set; }
        public string? passCode { get; set; }
    }
}
