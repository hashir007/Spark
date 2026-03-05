namespace SparkApp.APIModel.Organizer
{
    public class EventOrganizerUpdateRequest
    {
        public string name { get; set; } = null!;
        public string type { get; set; } = null!;
        public string email { get; set; } = null!;
    }
}
