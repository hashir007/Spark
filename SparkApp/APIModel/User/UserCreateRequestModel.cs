using System.ComponentModel.DataAnnotations;

namespace SparkApp.APIModel.User
{
    public class UserCreateRequestModel
    {
        [Required]
        public string username { get; set; } = null!;
        [Required]
        public string firstName { get; set; } = null!;
        [Required]
        public string lastName { get; set; } = null!;
        [Required]
        public string password { get; set; } = null!;
        [Required]
        public string email_address { get; set; } = null!;
        [Required]
        public string gender { get; set; } = null!;
        [Required]
        public string iam { get; set; } = null!;
        [Required]
        public string seeking { get; set; } = null!;
        [Required]
        public DateTime date_of_birth { get; set; }
        [Required]
        public string timezone { get; set; } = null!;
        [Required]
        public string language { get; set; } = null!;
        [Required]
        public string country { get; set; } = null!;
        [Required]
        public string state { get; set; } = null!;
        [Required]
        public string city { get; set; } = null!;
        [Required]
        public string zip { get; set; } = null!;
        [Required]
        public string race { get; set; } = null!;
        [Required]
        public string martialStatus { get; set; } = null!;
        [Required]
        public string bodyType { get; set; } = null!;
        [Required]
        public string height { get; set; } = null!;
        [Required]
        public string annualIncome { get; set; } = null!;
        [Required]
        public string profileHeadline { get; set; } = null!;
        [Required]
        public string aboutYourselfInYourOwnWords { get; set; } = null!;

        public string? describeThePersonYouAreLookingFor { get; set; }

        public string? photo {  get; set; } = null!; 
    }
}
