using PayPal.v1.CustomerDisputes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SparkService.ViewModels
{
    public class AuthorizeNetSubscriptionRequest
    {
        public OpaqueData opaqueData { get; set; }
        public Messages messages { get; set; }
        public EncryptedCardData encryptedCardData { get; set; }
        public CustomerInformation customerInformation { get; set; }


        public class CustomerInformation
        {
            public string firstName { get; set; }
            public string lastName { get; set; }
            public string address { get; set; }
            public string city { get; set; }
            public string zip { get; set; }
        }

        public class EncryptedCardData
        {
            public string cardNumber { get; set; }
            public string expDate { get; set; }
            public string bin { get; set; }
        }

        public class Message
        {
            public string code { get; set; }
            public string text { get; set; }
        }

        public class Messages
        {
            public string resultCode { get; set; }
            public List<Message> message { get; set; }
        }

        public class OpaqueData
        {
            public string dataDescriptor { get; set; }
            public string dataValue { get; set; }
        }

    }
}
