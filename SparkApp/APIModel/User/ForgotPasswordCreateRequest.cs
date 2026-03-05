namespace SparkApp.APIModel.User
{
    public class ForgotPasswordCreateRequestModel
    {
        public string email { get; set; } = null!;

        public string resetPasswordCallbackUrl { get; set; } = null!;
    }
}
