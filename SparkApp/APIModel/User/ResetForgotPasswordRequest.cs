using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc;

namespace SparkApp.APIModel.User
{
    public class ResetForgotPasswordRequest
    {
        [FromRoute]
        [BindRequired]
        public string token { get; set; } = null!;


        [FromBody]
        [BindRequired]
        public ResetForgotPassword ResetForgotPassword { get; set; } = null!;
    }

    public class ResetForgotPassword
    {
        public string password { get; set; } = null!;
    }
}
