namespace Server.Configurations
{
    public class JwtSettings
    {
        public string Key { get; set; } = "";
        public string Issuer { get; set; } = "";
        public string Audience { get; set; } = "";
        public int AccessTokenMinutes { get; set; } = 10;
        public int RefreshTokenDays { get; set; } = 14;
    }
}
