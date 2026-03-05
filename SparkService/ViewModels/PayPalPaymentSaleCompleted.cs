using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;


namespace SparkService.ViewModels
{
    public class PayPalPaymentSaleCompleted
    {
        public string id { get; set; }
        public DateTime create_time { get; set; }
        public string resource_type { get; set; }
        public string event_type { get; set; }
        public string summary { get; set; }
        public Resource resource { get; set; }
        public string status { get; set; }
        public List<Transmission> transmissions { get; set; }
        public List<Link> links { get; set; }
        public string event_version { get; set; }


        public class TransactionFee
        {
            public string currency { get; set; }
            public string value { get; set; }
        }

        public class Transmission
        {
            public string webhook_url { get; set; }
            public int http_status { get; set; }
            public string reason_phrase { get; set; }
            public ResponseHeaders response_headers { get; set; }
            public string transmission_id { get; set; }
            public string status { get; set; }
            public DateTime timestamp { get; set; }
        }

        public class Amount
        {
            public string total { get; set; }
            public string currency { get; set; }
            public Details details { get; set; }
        }

        public class Details
        {
            public string subtotal { get; set; }
        }

        public class Link
        {
            public string method { get; set; }
            public string rel { get; set; }
            public string href { get; set; }
            public string encType { get; set; }
        }

        public class Resource
        {
            public Amount amount { get; set; }
            public string payment_mode { get; set; }
            public DateTime create_time { get; set; }
            public string custom { get; set; }
            public TransactionFee transaction_fee { get; set; }
            public string billing_agreement_id { get; set; }
            public DateTime update_time { get; set; }
            public string protection_eligibility_type { get; set; }
            public string protection_eligibility { get; set; }
            public List<Link> links { get; set; }
            public string id { get; set; }
            public string state { get; set; }
            public string invoice_number { get; set; }
        }

        public class ResponseHeaders
        {
            public string Server { get; set; }

            [JsonProperty("Access-Control-Allow-Origin")]
            public string AccessControlAllowOrigin { get; set; }
            public string Connection { get; set; }
            public string Pragma { get; set; }
            public string Date { get; set; }

            [JsonProperty("Cache-Control")]
            public string CacheControl { get; set; }
            public string ETag { get; set; }

            [JsonProperty("Set-Cookie")]
            public string SetCookie { get; set; }
            public string Expires { get; set; }

            [JsonProperty("Surrogate-Control")]
            public string SurrogateControl { get; set; }

            [JsonProperty("Content-Length")]
            public string ContentLength { get; set; }

            [JsonProperty("Front-End-Https")]
            public string FrontEndHttps { get; set; }

            [JsonProperty("X-Powered-By")]
            public string XPoweredBy { get; set; }

            [JsonProperty("Content-Type")]
            public string ContentType { get; set; }
        }


    }
}
