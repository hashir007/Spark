using SparkService.ViewModels;
using Microsoft.AspNetCore.Components.Web;
using MongoDB.Driver.Core.Misc;

namespace SparkApp.APIModel.User
{
    public class AuthenticateResponseModel
    {
        public string token { get; set; } = null!;
        public string refreshToken { get; set; } = null!;
        public DateTime expires { get; set; }
        public string username { get; set; } = null!;
        public string email_address { get; set; } = null!;      
        public bool is_email_verified { get; set; }
        public bool is_active { get; set; }
        public string timezone { get; set; } = null!;
        public string language { get; set; } = null!;        
        public bool is_verification_sent { get; set; } 

        public string id { get;set; } = null!;

        public SubscriptionV3ViewModel? subscription { get; set; } = null!;

    }
}
