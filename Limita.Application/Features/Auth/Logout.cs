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
    /// <summary>Ends the session that owns the given refresh token. Always succeeds, so it is safe to call twice.</summary>
    public static class Logout
    {
        public sealed record Command(string RefreshToken) : ICommand;

        internal sealed class Validator : AbstractValidator<Command>
        {
            public Validator() => RuleFor(x => x.RefreshToken).NotEmpty().MaximumLength(200);
        }

        internal sealed class Handler(
            IApplicationDbContext db,
            ISecretHasher secretHasher,
            ICurrentUser currentUser,
            TimeProvider time) : ICommandHandler<Command>
        {
            public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
            {
                if (currentUser.UserId is not { } userId)
                    return Result.Failure(AuthErrors.Unauthenticated);

                var hash = secretHasher.Hash(request.RefreshToken);

                // A user can only end their own sessions.
                var token = await db.RefreshTokens
                    .FirstOrDefaultAsync(t => t.TokenHash == hash && t.UserId == userId, cancellationToken);

                token?.Revoke(time.GetUtcNow());

                return Result.Success();
            }
        }




    }
}
