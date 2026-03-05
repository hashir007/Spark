using System.ComponentModel.DataAnnotations;

namespace SparkApp.APIModel.Profile
{
    public class InterestedInRequest
    {
        [Required]
        public string iam { get; set; } = null!;
        [Required]
        public string seeking { get; set; } = null!;

        [Required]
        public string relationshipGoals { get; set; } = null!;
    }
}
