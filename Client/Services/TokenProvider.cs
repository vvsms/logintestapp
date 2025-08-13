namespace Client.Services
{
    public class TokenProvider
    {
        private string? _accessToken;
        private DateTime _expiresAtUtc = DateTime.MinValue;

        public string? AccessToken => _accessToken;
        public DateTime ExpiresAtUtc => _expiresAtUtc;

        public void SetToken(string token, DateTime expiresAtUtc)
        {
            _accessToken = token;
            _expiresAtUtc = expiresAtUtc;
        }

        public void Clear()
        {
            _accessToken = null;
            _expiresAtUtc = DateTime.MinValue;
        }

        public bool HasValidToken() =>
            !string.IsNullOrEmpty(_accessToken) && _expiresAtUtc > DateTime.UtcNow.AddSeconds(5);
    }
}