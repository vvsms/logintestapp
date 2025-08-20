using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedDtos
{
    public sealed class RegisterRequest
    {
        public string Email { get; set; } = default!;
        public string Password { get; set; } = default!;
        public string FullName { get; set; } = default!;
    }

    public sealed class LoginRequest
    {
        public string Email { get; set; } = default!;
        public string Password { get; set; } = default!;
    }

    public sealed class AuthResponse
    {
        public string AccessToken { get; set; } = default!;
        public DateTime ExpiresAtUtc { get; set; }
        public string? Email { get; set; }
        public string? FullName { get; set; }
        public IEnumerable<string> Roles { get; set; } = Array.Empty<string>();
    }
}
