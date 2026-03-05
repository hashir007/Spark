using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SparkService
{
    public class AuthorizeNetOptions
    {
        public string ApiLoginID { get; set; } = null!;
        public string ApiTransactionKey { get; set; } = null!;
        public string BaseUrl { get; set; } = null!;
        public string Mode { get; set; } = null!;
        public string X_ANET_Signature { get; set; } = null!;
    }
}
