using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SparkService.ViewModels
{
    public class AuthorizeNetCustomerSubscriptionCancelled
    {
        public string notificationId { get; set; }
        public string eventType { get; set; }
        public DateTime eventDate { get; set; }
        public string webhookId { get; set; }
        public JObject payload { get; set; }
    }
}
