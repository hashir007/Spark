using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SparkService.ViewModels
{
    public class ProfileViewModelV3
    {
        public string city { get; set; } = null!;
        public string state { get; set; } = null!;
        public string country { get; set; } = null!;
        public string zip_code { get; set; } = null!;
        public FileViewModel? photo { get; set; } = null!;
        public string? gender { get; set; }
        public int? age { get; set; }
    }
}
