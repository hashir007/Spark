using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace SparkApp.APIModel.SubscriptionServices
{
    public class SubscriptionServicesCreateRequest
    {
        [BindRequired]
        public string? name { get; set; }
    }
}
