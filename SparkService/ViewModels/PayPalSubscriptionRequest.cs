using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SparkService.ViewModels
{
    public class PayPalSubscriptionRequest
    {
        public string orderID { get; set; }
        public string subscriptionID { get; set; }
        public string facilitatorAccessToken { get; set; }
        public string paymentSource { get; set; }
    }
}
