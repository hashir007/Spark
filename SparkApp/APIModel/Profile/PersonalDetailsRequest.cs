using System.ComponentModel.DataAnnotations;

namespace SparkApp.APIModel.Profile
{
    public class PersonalDetailsRequest
    {
        [Required]
        public string first_name { get; set; } = null!;
        [Required]
        public string last_name { get; set; } = null!;
        [Required]
        public DateTime date_of_birth { get; set; }
        [Required]
        public string phone_number { get; set; } = null!;      
        [Required]
        public string gender { get; set; } = null!;
        [Required]
        public string race { get; set; } = null!;
        [Required]
        public string height { get; set; } = null!;
        [Required]
        public string martialStatus { get; set; } = null!;
        [Required]
        public string annualIncome { get; set; } = null!;
        [Required]
        public string bodyType { get; set; } = null!;
    }
}
