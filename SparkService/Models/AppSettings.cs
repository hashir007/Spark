namespace SparkService.Models
{
    public class AppSettings
    {
        public string JWTSecret { get; set; } = null!;
        public string JWTValidIssuer { get; set; } = null!;
        public string JWTValidAudience { get; set; } = null!;
        public int JWTTokenValidityInMinutes { get; set; }
        public string Encryption { get; set; } = null!;
        public int RefreshTokenExpiryTimeDays { get; set; }
        public string ClientUrl { get; set; } = null!;
        public int ForgotPasswordExpireTimeDays { get; set; }
    }
}
