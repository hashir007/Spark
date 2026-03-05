using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace SparkService.ViewModels
{
    public class KissesViewModel
    {
       
        public string? Id { get; set; }      
        public string user_id { get; set; } = null!;
        public UserViewModelV2 user { get; set; } = null!;        
        public string kissed_id { get; set; } = null!;
        public UserViewModelV2 kissed { get; set; } = null!;
        public int kissed_count { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime created_at { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime updated_at { get; set; }
    }
}
