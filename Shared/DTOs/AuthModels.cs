namespace Shared.DTOs;


    public record AuthResponse(string AccessToken, DateTime ExpiresAt, string[] Roles, string[] Policies);
