using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Limita.Infrastructure.Identity
{
    public sealed class JwtOptions
    {
        public const string SectionName = "Jwt";

        public string Issuer { get; init; } = string.Empty;
        public string Audience { get; init; } = string.Empty;

        /// <summary>Signing key, at least 32 characters. Never commit a real key: use user-secrets or environment variables.</summary>
        public string SecretKey { get; init; } = string.Empty;

        public int ExpiryMinutes { get; init; } = 15;

        public int RefreshTokenDays { get; init; } = 7;

    }
}
