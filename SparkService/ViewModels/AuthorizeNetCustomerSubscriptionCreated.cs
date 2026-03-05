using AuthorizeNet.Api.Contracts.V1;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static SparkService.ViewModels.AuthorizeNetPaymentAuthCaptureCreated;

namespace SparkService.ViewModels
{
    public class AuthorizeNetCustomerSubscriptionCreated
    {
        public string notificationId { get; set; }
        public string eventType { get; set; }
        public DateTime eventDate { get; set; }
        public string webhookId { get; set; }
        public JObject payload { get; set; }

    }
}
