using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc;

namespace SparkApp.APIModel.User
{
    public class LikesOrDisLikesCreateRequest
    {
        [FromRoute]
        [BindRequired]
        public string id { get; set; } = null!;

        [FromRoute]
        [BindRequired]
        public string photoId { get; set; } = null!;

        [FromBody]
        [BindRequired]
        public LikesOrDisLikesPhoto LikesOrDisLikesPhoto {  get; set; }
    }

    public class LikesOrDisLikesPhoto
    {
        public bool IsLike { get; set; }

        public bool IsDislike { get; set; }
    }
}
