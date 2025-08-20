namespace ServerApi.Infrastructure.Identity
{
    public class UserRefreshToken
    {
        public int Id { get; set; }
        public string Token { get; set; } = default!;
        public DateTime ExpiresAtUtc { get; set; }
        public bool Revoked { get; set; }
        public string UserId { get; set; } = default!;
        public ApplicationUser User { get; set; } = default!;
    }
}
