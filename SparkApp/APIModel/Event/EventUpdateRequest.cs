using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace SparkApp.APIModel.Event
{
    public class EventUpdateRequest
    {
        [BindRequired]
        public string name { get; set; } = null!;
        [BindRequired]
        public DateTime start_date { get; set; }
        [BindRequired]
        public string venueId { get; set; } = null!;
        [BindRequired]
        public string description { get; set; } = null!;
        [BindRequired]
        public string type { get; set; } = null!;
        [BindRequired]
        public string organizerId { get; set; } = null!;
        [BindRequired]
        public int capacity { get; set; }
        [BindRequired]
        public int attendees { get; set; }
        [BindRequired]
        public string status { get; set; } = null!;
    }
}
