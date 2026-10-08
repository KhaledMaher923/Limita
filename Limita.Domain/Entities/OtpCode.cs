using Limita.Domain.Common;
using Limita.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Limita.Domain.Entities
{
    /// <summary>A one-time code for phone verification or password reset. Only a hash of the code is stored.</summary>
    public sealed class OtpCode : Entity
    {
        private OtpCode() // required by EF Core
        {
        }

        public Guid UserId { get; private set; }
        public OtpPurpose Purpose { get; private set; }
        public string CodeHash { get; private set; } = string.Empty;
        public const int MaxFailedAttempts = 5;

        public DateTimeOffset CreatedAt { get; private set; }
        public DateTimeOffset ExpiresAt { get; private set; }
        public DateTimeOffset? ConsumedAt { get; private set; }
        public int FailedAttempts { get; private set; }

        public static OtpCode Create(Guid userId, OtpPurpose purpose, string codeHash, DateTimeOffset expiresAt, DateTimeOffset now)
        {
            if (expiresAt <= now)
                throw new DomainException("A code must expire in the future.");

            return new OtpCode
            {
                UserId = userId,
                Purpose = purpose,
                CodeHash = Guard.NotBlank(codeHash, "Code hash", 128),
                CreatedAt = now,
                ExpiresAt = expiresAt
            };
        }

        public bool IsValid(DateTimeOffset now)
            => ConsumedAt is null && ExpiresAt > now && FailedAttempts < MaxFailedAttempts;

        public void Consume(DateTimeOffset now)
        {
            if (!IsValid(now))
                throw new DomainException("The code is invalid or has expired.");

            ConsumedAt = now;
        }

        /// <summary>Counts a wrong guess. After MaxFailedAttempts the code can no longer be used.</summary>
        public void RegisterFailedAttempt() => FailedAttempts++;

        /// <summary>Retires a code that was replaced by a newer one.</summary>
        public void Invalidate(DateTimeOffset now) => ConsumedAt ??= now;

    }
}
