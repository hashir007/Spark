using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SparkService
{
    public class PaypalOptions
    {
        public string ClientId { get; set; } = null!;
        public string ClientSecret { get; set; } = null!;
        public string BaseUrl { get; set; } = null!;
        public string Mode { get; set; } = null!;
        public string WebhookSecret { get; set; } = null!;
    }
}
