using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace SparkApp.APIModel.Subscribe
{
    public class SubscribeCreateRequest
    {
        [BindRequired]
        public string source { get; set; } = null!;
        [BindRequired]
        public string data { get; set; } = null!;

        [BindRequired]
        public string subscriptionPlansId { get; set; } = null!;
    }
}
