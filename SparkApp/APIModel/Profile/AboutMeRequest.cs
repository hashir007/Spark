using System.ComponentModel.DataAnnotations;

namespace SparkApp.APIModel.Profile
{
    public class AboutMeRequest
    {
        [Required]
        public string bio { get; set; } = null!;

        [Required]
        public string aboutYourselfInYourOwnWords { get; set; } = null!;
        [Required]
        public string describeThePersonYouAreLookingFor { get; set; } = null!;
        [Required]
        public string profileHeadline { get; set; } = null!;
    }
}
