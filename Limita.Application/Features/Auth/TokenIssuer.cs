using Limita.Application.Common.Abstractions;
using Limita.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Limita.Application.Features.Auth
{
    /// <summary>Issues access and refresh tokens and ends sessions. Only refresh token hashes are stored.</summary>
    internal sealed class TokenIssuer(
        IApplicationDbContext db,
        IJwtTokenGenerator jwt,
        ISecretGenerator generator,
        ISecretHasher hasher,
        TimeProvider time)
    {
        public AuthTokens Issue(User user)
        {
            var now = time.GetUtcNow();
            var access = jwt.CreateAccessToken(user, now);

            var refreshToken = generator.NewRefreshToken();
            var refreshExpiresAt = now.Add(jwt.RefreshTokenLifetime);

            db.RefreshTokens.Add(RefreshToken.Create(user.Id, hasher.Hash(refreshToken), refreshExpiresAt, now));

            return new AuthTokens(access.Value, access.ExpiresAt, refreshToken, refreshExpiresAt);
        }

        public async Task RevokeAllAsync(Guid userId, CancellationToken cancellationToken)
        {
            var now = time.GetUtcNow();

            var active = await db.RefreshTokens
                .Where(t => t.UserId == userId && t.RevokedAt == null)
                .ToListAsync(cancellationToken);

            foreach (var token in active)
                token.Revoke(now);
        }




    }
}
