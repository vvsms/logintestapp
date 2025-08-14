using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Server.Models
{
    public class RefreshToken
    {
        [Key] public int Id { get; set; }

        [Required] public string Token { get; set; } = string.Empty;

        [Required] public DateTime ExpiryDate { get; set; }

        [Required] public string UserId { get; set; } = string.Empty;

        [ForeignKey(nameof(UserId))] public ApplicationUser? User { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string CreatedByIp { get; set; } = string.Empty;

        public DateTime? RevokedAt { get; set; }
        public string? RevokedByIp { get; set; }
        public string? ReplacedByToken { get; set; }

        [NotMapped] public bool IsExpired => DateTime.UtcNow >= ExpiryDate;
        [NotMapped] public bool IsActive => RevokedAt == null && !IsExpired;
    }
}
