namespace Client.Auth
{
    public interface IAccessTokenStore
    {
        string? AccessToken { get; }
        DateTimeOffset? AccessTokenExpiresAt { get; }
        void SetToken(string token, DateTimeOffset expiresAt);
        void Clear();
        event Action? Changed;
    }

    public sealed class MemoryAccessTokenStore : IAccessTokenStore
    {
        private string? _token;
        private DateTimeOffset? _exp;
        public string? AccessToken => _token;
        public DateTimeOffset? AccessTokenExpiresAt => _exp;
        public event Action? Changed;

        public void SetToken(string token, DateTimeOffset expiresAt)
        {
            _token = token;
            _exp = expiresAt;
            Changed?.Invoke();
        }
        public void Clear()
        {
            _token = null; _exp = null;
            Changed?.Invoke();
        }
    }
}