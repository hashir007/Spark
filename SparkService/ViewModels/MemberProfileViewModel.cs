using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

namespace SparkService.ViewModels
{
    public class MemberProfileViewModel
    {       
        public string bio { get; set; } = null!;
        public string? gender { get; set; }
        public string? iam { get; set; }
        public string? seeking { get; set; }
        public string? educationLevel { get; set; }
        public string? relationshipGoals { get; set; }      
        public int? age { get; set; }
        public string race { get; set; } = null!;
        public string martialStatus { get; set; } = null!;
        public string height { get; set; } = null!;
        public string annualIncome { get; set; } = null!;
        public string bodyType { get; set; } = null!;
        public string city { get; set; } = null!;
        public string state { get; set; } = null!;
        public string zip_code { get; set; } = null!;
        public string country { get; set; } = null!;
        public string aboutYourselfInYourOwnWords { get; set; } = null!;
        public string describeThePersonYouAreLookingFor { get; set; } = null!;
        public string profileHeadline { get; set; } = null!;
        public FileViewModel? photo { get; set; } = null!;
    }
}
