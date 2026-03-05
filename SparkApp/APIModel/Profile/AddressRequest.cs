using System.ComponentModel.DataAnnotations;

namespace SparkApp.APIModel.Profile
{
    public class AddressRequest
    {
        [Required]
        public string address { get; set; } = null!;
        [Required]
        public string address2 { get; set; } = null!;
        [Required]
        public string city { get; set; } = null!;
        [Required]
        public string state { get; set; } = null!;
        [Required]
        public string zip_code { get; set; } = null!;
        [Required]
        public string country { get; set; } = null!;
    }
}
