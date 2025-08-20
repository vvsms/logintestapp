namespace ClientApp.State
{
    public class TokenState
    {
        public string? AccessToken { get; private set; }
        public DateTime ExpiresAtUtc { get; private set; }

        public bool IsExpiredSoon(TimeSpan margin) => DateTime.UtcNow >= ExpiresAtUtc - margin;

        public void Set(string token, DateTime expiresAtUtc)
        {
            AccessToken = token;
            ExpiresAtUtc = expiresAtUtc;
        }

        public void Clear()
        {
            AccessToken = null;
            ExpiresAtUtc = DateTime.MinValue;
        }
    }
}
