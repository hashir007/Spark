using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc;

namespace SparkApp.APIModel.User
{
    public class ViewsPhotoCreateRequest
    {
        [FromRoute]
        [BindRequired]
        public string id { get; set; } = null!;

        [FromRoute]
        [BindRequired]
        public string photoId { get; set; } = null!;
    }
}
