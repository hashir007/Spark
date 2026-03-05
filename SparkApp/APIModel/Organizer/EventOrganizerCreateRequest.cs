using MongoDB.Bson.Serialization.Attributes;

namespace SparkApp.APIModel.Organizer
{
    public class EventOrganizerCreateRequest
    {
        public string name { get; set; } = null!;
        public string type { get; set; } = null!;
        public string email { get; set; } = null!;
    }
}
