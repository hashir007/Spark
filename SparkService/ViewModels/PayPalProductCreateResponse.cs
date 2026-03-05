using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static SparkService.ViewModels.PayPalProductListResponse;

namespace SparkService.ViewModels
{
    public class PayPalProductCreateResponse
    {
        public string? id { get; set; }
        public string? name { get; set; }
        public string? description { get; set; }
        public string? type { get; set; }
        public string? category { get; set; }
        public string? image_url { get; set; }
        public string? home_url { get; set; }
     
        public List<Link>? links { get; set; }


        public class Link
        {
            public string? href { get; set; }
            public string? rel { get; set; }
            public string? method { get; set; }
        }

    }
}
