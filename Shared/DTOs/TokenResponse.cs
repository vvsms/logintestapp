using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.DTOs
{
    public class TokenResponse
    {
        public string AccessToken { get; set; } = "";
        public int ExpiresInMinutes { get; set; }
    }
}
