namespace SparkApp.APIModel.Administrators
{
    public class AuthenticateResponseModel2
    {
        public string id { get; set; } = null!;
        public string token { get; set; } = null!;
        public string refreshToken { get; set; } = null!;
        public DateTime expires { get; set; }
        public string username { get; set; } = null!;
        public string permissions { get; set; } = null!;
        public bool is_active { get; set; }
        public string email { get; set; } = null!;
        public string phone { get; set; } = null!;
    }
}
