using System.ComponentModel.DataAnnotations;

namespace SparkApp.APIModel.User
{
    public class ChangePasswordRequestModel
    {
        [Required]
        public string OldPassword { get; set; } = null!;
        [Required]
        public string NewPassword { get; set; } = null!;
    }
}
