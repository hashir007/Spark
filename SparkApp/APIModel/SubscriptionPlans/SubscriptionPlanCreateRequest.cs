using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace SparkApp.APIModel.SubscriptionPlans
{
    public class SubscriptionPlanCreateRequest
    {
        [BindRequired]
        public string name { get; set; } = null!;
        [BindRequired]
        public string description { get; set; } = null!;
        [BindRequired]
        public string descriptionHTML { get; set; } = null!;
        [BindRequired]
        public string type { get; set; } = null!;
        [BindRequired]
        public decimal price { get; set; }

        [BindRequired]
        public int order { get; set; }


        [BindRequired]
        public string colour { get; set; } = null!;

        [BindRequired]
        public long storage { get; set; } 

        [BindRequired]
        public List<string> services { get; set; }

        public SubscriptionPlanCreateRequest()
        {
            this.services = new List<string>();
        }
    }
}
