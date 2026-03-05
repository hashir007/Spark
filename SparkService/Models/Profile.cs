using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using AuthorizeNet.Api.Contracts.V1;

namespace SparkService.Models
{
    public class Profile
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        [BsonElement("_userid")]
        public string UserId { get; set; } = null!;
        public string first_name { get; set; } = null!;
        public string last_name { get; set; } = null!;
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime? date_of_birth { get; set; }
        public string bio { get; set; } = null!;
        public string gender { get; set; } = null!;
        public string iam { get; set; } = null!;
        public string seeking { get; set; } = null!;
        public string educationLevel { get; set; } = null!;
        public string relationshipGoals { get; set; } = null!;
        public string height { get; set; } = null!;
        public int height_in_inches { get; set; }
        public string race { get; set; } = null!;
        public string martialStatus { get; set; } = null!;
        public string annualIncome { get; set; } = null!;
        public string bodyType { get; set; } = null!;
        public string address { get; set; } = null!;
        public string address2 { get; set; } = null!;
        public string city { get; set; } = null!;
        public string state { get; set; } = null!;
        public string zip_code { get; set; } = null!;
        public string country { get; set; } = null!;
        public string phone_number { get; set; } = null!;
        public string profileHeadline { get; set; } = null!;
        public string aboutYourselfInYourOwnWords { get; set; } = null!;
        public string describeThePersonYouAreLookingFor { get; set; } = null!;
        [BsonRepresentation(BsonType.ObjectId)]
        public string? photo { get; set; } = null!;

        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime created_at { get; set; }
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime updated_at { get; set; }
    }
}
