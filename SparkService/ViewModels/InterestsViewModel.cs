using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SparkService.ViewModels
{
    public class InterestsViewModel
    {
        public string? Id { get; set; }
        public string interest_description { get; set; } = null!;

        public string category_id { get; set; } = null!;
        public InterestCategoriesViewModel category { get; set; } = null!;

        public int popularity { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime created_at { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime modified_at { get; set; }

        public string? created_by { get; set; }
        public UserViewModelV2? created_by_user { get; set; }
      
        public string? modified_by { get; set; }
        public UserViewModelV2? modified_by_user { get; set; }

        public bool is_active { get; set; }
        public bool is_featured { get; set; }

    }
}
