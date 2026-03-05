using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SparkService.ViewModels
{
    public class PayPalProductListResponse
    {
        public int total_items { get; set; }
        public int total_pages { get; set; }
        public List<Product> products { get; set; }


        public class Link
        {
            public string href { get; set; }
            public string rel { get; set; }
            public string method { get; set; }
        }

        public class Product
        {
            public string id { get; set; }
            public string name { get; set; }
            public string description { get; set; }           
            public List<Link> links { get; set; }
        }
    }
}
