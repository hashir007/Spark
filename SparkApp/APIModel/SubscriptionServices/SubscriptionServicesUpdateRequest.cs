using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace SparkApp.APIModel.SubscriptionServices
{
    public class SubscriptionServicesUpdateRequest
    {
        [BindRequired]
        public string? name { get; set; }
    }
}
