namespace Shared.DTOs;


    public record LoginRequest(string Email, string Password);
    public record AuthResponse(string AccessToken, DateTime ExpiresAt, string[] Roles, string[] Policies);
