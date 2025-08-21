namespace ClientApp.State
{
    public class TokenState
    {
        public string? Email { get; private set; }
        public string? FullName { get; private set; }
        public string[] Roles { get; private set; } = [];

        public bool IsAuthenticated => !string.IsNullOrEmpty(Email);

        public event Action? Changed;

        public void SetUser(string email, string fullName, string[] roles)
        {
            Email = email;
            FullName = fullName;
            Roles = roles ?? [];
            Changed?.Invoke();
        }

        public void Clear()
        {
            Email = null;
            FullName = null;
            Roles = [];
            Changed?.Invoke();
        }
    }
}