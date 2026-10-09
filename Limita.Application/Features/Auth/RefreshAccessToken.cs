using FluentValidation;
using Limita.Application.Common;
using Limita.Application.Common.Abstractions;
using Limita.Application.Common.Messaging;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Limita.Application.Features.Auth
{
    /// <summary>
    /// Exchanges a refresh token for a new access token and a new refresh token (rotation).
    /// If an already used token is presented again, every session of that user is ended (ICommitOnFailure keeps the revocations).
    /// </summary>
    public static class RefreshAccessToken
    {
        public sealed record Command(string RefreshToken) : ICommand<AuthTokens>, ICommitOnFailure;

        internal sealed class Validator : AbstractValidator<Command>
        {
            public Validator() => RuleFor(x => x.RefreshToken).NotEmpty().MaximumLength(200);
        }

        internal sealed class Handler(
            IApplicationDbContext db,
            ISecretHasher secretHasher,
            TokenIssuer issuer,
            TimeProvider time) : ICommandHandler<Command, AuthTokens>
        {
            public async Task<Result<AuthTokens>> Handle(Command request, CancellationToken cancellationToken)
            {
                var hash = secretHasher.Hash(request.RefreshToken);
                var token = await db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);

                if (token is null)
                    return AuthErrors.InvalidRefreshToken;

                var now = time.GetUtcNow();

                if (token.RevokedAt is not null)
                {
                    // A used token came back: assume it was stolen and end every session of this user.
                    await issuer.RevokeAllAsync(token.UserId, cancellationToken);
                    return AuthErrors.InvalidRefreshToken;
                }

                if (!token.IsActive(now))
                    return AuthErrors.InvalidRefreshToken;

                var user = await db.Users.FirstOrDefaultAsync(u => u.Id == token.UserId, cancellationToken);
                if (user is null)
                    return AuthErrors.InvalidRefreshToken;

                token.Revoke(now);

                return issuer.Issue(user);
            }
        }


    }
}
