namespace Client.Services
{
    public interface IAuthService
    {
        // Returns true if silent refresh succeeded and token available
        Task<bool> TrySilentRefreshAsync();

        // Login with credentials (server sets refresh cookie + returns access token)
        Task<bool> LoginAsync(string email, string password);

        // Logout (server revokes refresh tokens and clears cookie)
        Task LogoutAsync();

        // Return access token if present (in-memory)
        string? GetAccessToken();
    }
}
