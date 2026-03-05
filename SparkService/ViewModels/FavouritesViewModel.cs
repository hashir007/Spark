using MongoDB.Bson.Serialization.Attributes;

namespace SparkService.ViewModels
{
    public class FavoritessViewModel
    {
        public string? Id { get; set; }
        public string user_id { get; set; } = null!;
        public UserViewModelV2 user { get; set; } = null!;
        public string favorite_id { get; set; } = null!;
        public UserViewModelV2 favorite { get; set; } = null!;

        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime created_at { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime updated_at { get; set; }
    }
}
