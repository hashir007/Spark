using SparkService.Models;

namespace SparkService.ViewModels
{
    public class UserTraitsViewModel
    {
        public string user_id { get; set; } = null!;
        public UserViewModelV2 user { get; set; } = null!;
        public string trait_id { get; set; } = null!;
        public Traits trait { get; set; } = null!;
        public int trait_value { get; set; }
    }
}
