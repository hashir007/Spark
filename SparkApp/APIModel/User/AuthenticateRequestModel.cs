using System.ComponentModel.DataAnnotations;

namespace SparkApp.APIModel.User
{
    public class AuthenticateRequestModel
    {
        [Required]
        public string UserName { get; set; } = null!;
        [Required]
        public string Password { get; set; } = null!;
    }
}
