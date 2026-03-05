using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SparkService.ViewModels
{
    public class PayPalBillingSubscriptionSuspended
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
        public string resource_version { get; set; }


        public class ShippingAmount
        {
            public string currency_code { get; set; }
            public string value { get; set; }
        }

        public class Subscriber
        {
            public string email_address { get; set; }
            public string payer_id { get; set; }
            public Name name { get; set; }
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
            public string currency_code { get; set; }
            public string value { get; set; }
        }

        public class BillingInfo
        {
            public OutstandingBalance outstanding_balance { get; set; }
            public List<CycleExecution> cycle_executions { get; set; }
            public LastPayment last_payment { get; set; }
            public int failed_payments_count { get; set; }
        }

        public class CycleExecution
        {
            public string tenure_type { get; set; }
            public int sequence { get; set; }
            public int cycles_completed { get; set; }
            public int cycles_remaining { get; set; }
            public int current_pricing_scheme_version { get; set; }
            public int total_cycles { get; set; }
        }

        public class LastPayment
        {
            public Amount amount { get; set; }
            public DateTime time { get; set; }
        }

        public class Link
        {
            public string href { get; set; }
            public string rel { get; set; }
            public string method { get; set; }
            public string encType { get; set; }
        }

        public class Name
        {
            public string given_name { get; set; }
            public string surname { get; set; }
        }

        public class OutstandingBalance
        {
            public string currency_code { get; set; }
            public string value { get; set; }
        }

        public class Resource
        {
            public string status_change_note { get; set; }
            public string quantity { get; set; }
            public Subscriber subscriber { get; set; }
            public DateTime create_time { get; set; }
            public string custom_id { get; set; }
            public bool plan_overridden { get; set; }
            public ShippingAmount shipping_amount { get; set; }
            public DateTime start_time { get; set; }
            public DateTime update_time { get; set; }
            public BillingInfo billing_info { get; set; }
            public List<Link> links { get; set; }
            public string id { get; set; }
            public string plan_id { get; set; }
            public string status { get; set; }
            public DateTime status_update_time { get; set; }
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
