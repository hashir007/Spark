using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc;

namespace SparkApp.APIModel.Member
{
    public class SearchRequestModel
    {

        [FromRoute]
        [BindRequired]
        public string id { get; set; } = null!;

        [FromRoute]
        [BindRequired]
        public int page { get; set; }

        [FromRoute]
        [BindRequired]
        public int pageSize { get; set; }


        [FromBody]
        [BindRequired]
        public SearchRequest SearchRequest { get; set; } = null!;
    }

    public class SearchRequest
    {
        public string? iam { get; set; }
        public string? seeking { get; set; }
        public string? ageFrom { get; set; }
        public string? ageTo { get; set; }
        public string[]? race { get; set; }
        public string[]? gender { get; set; }
        public string[]? educationLevel { get; set; }
        public string? heightFrom { get; set; }
        public string? heightTo { get; set; }
        public string? martialStatus { get; set; }
        public string[]? income { get; set; }
        public string[]? bodyType { get; set; }
        public string[]? country { get; set; }
        public string? userCriteria { get; set; }
    }
}
