using Limita.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Limita.Domain.Entities
{
    /// <summary>Only a hash of the token is stored, never the token itself.</summary>
    public sealed class RefreshToken : Entity
    {
        private RefreshToken() // required by EF Core
        {
        }

        public Guid UserId { get; private set; }
        public string TokenHash { get; private set; } = string.Empty;
        public DateTimeOffset ExpiresAt { get; private set; }
        public DateTimeOffset CreatedAt { get; private set; }
        public DateTimeOffset? RevokedAt { get; private set; }

        public static RefreshToken Create(Guid userId, string tokenHash, DateTimeOffset expiresAt, DateTimeOffset now)
        {
            if (expiresAt <= now)
                throw new DomainException("A refresh token must expire in the future.");

            return new RefreshToken
            {
                UserId = userId,
                TokenHash = Guard.NotBlank(tokenHash, "Token hash", 128),
                ExpiresAt = expiresAt,
                CreatedAt = now
            };
        }

        public bool IsActive(DateTimeOffset now) => RevokedAt is null && ExpiresAt > now;

        public void Revoke(DateTimeOffset now) => RevokedAt ??= now;

    }
}
