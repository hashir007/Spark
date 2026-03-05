using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace SparkApp.APIModel.Venue
{
    public class VenueUpdateRequest
    {
        [BindRequired]
        public string name { get; set; } = null!;
        [BindRequired]
        public string street { get; set; } = null!;
        [BindRequired]
        public string street2 { get; set; } = null!;
        [BindRequired]
        public string city { get; set; } = null!;
        [BindRequired]
        public string state { get; set; } = null!;
        [BindRequired]
        public string zip { get; set; } = null!;
        [BindRequired]
        public string country { get; set; } = null!;
        [BindRequired]
        public string manager_name { get; set; } = null!;
        [BindRequired]
        public string manager_phone { get; set; } = null!;
        [BindRequired]
        public string manager_email { get; set; } = null!;       
    }
}
