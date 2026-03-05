using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc;

namespace SparkApp.APIModel.User
{
    public class UserPhotoSearchRequest
    {
        [FromRoute]
        [BindRequired]
        public string id { get; set; }

        [FromRoute]
        [BindRequired]
        public int page { get; set; }

        [FromRoute]
        [BindRequired]
        public int pageSize { get; set; }

        [FromBody]
        public PhotoSearch? PhotoSearch { get; set; }

    }

    public class PhotoSearch
    {
        public string? term {  get; set; }
    }
}
