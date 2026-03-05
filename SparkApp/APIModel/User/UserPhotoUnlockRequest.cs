using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc;

namespace SparkApp.APIModel.User
{
    public class UserPhotoUnlockRequest
    {
        [FromRoute]
        [BindRequired]
        public string id { get; set; } = null!;


        [FromRoute]
        [BindRequired]
        public string photoId { get; set; } = null!;


        [FromBody]
        [BindRequired]
        public UnlockCredentials UnlockCredentials { get; set; } = null!;     

    }
    public class UnlockCredentials
    {
        public string code { get; set; } = null!;
    }
}
