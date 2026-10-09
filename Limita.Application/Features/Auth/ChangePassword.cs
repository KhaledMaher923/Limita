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
    /// <summary>Changes the password of the signed-in user and ends all their refresh-token sessions.</summary>
    public static class ChangePassword
    {
        public sealed record Command(string CurrentPassword, string NewPassword) : ICommand;

        internal sealed class Validator : AbstractValidator<Command>
        {
            public Validator()
            {
                RuleFor(x => x.CurrentPassword).NotEmpty().MaximumLength(128);
                RuleFor(x => x.NewPassword).StrongPassword()
                    .NotEqual(x => x.CurrentPassword).WithMessage("The new password must be different from the current one.");
            }
        }

        internal sealed class Handler(
            IApplicationDbContext db,
            IPasswordHasher hasher,
            TokenIssuer issuer,
            ICurrentUser currentUser) : ICommandHandler<Command>
        {
            public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
            {
                if (currentUser.UserId is not { } userId)
                    return Result.Failure(AuthErrors.Unauthenticated);

                var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
                if (user is null)
                    return Result.Failure(AuthErrors.UserNotFound);

                if (!hasher.Verify(request.CurrentPassword, user.PasswordHash))
                    return Result.Failure(AuthErrors.CurrentPasswordIncorrect);

                user.ChangePassword(hasher.Hash(request.NewPassword));
                await issuer.RevokeAllAsync(user.Id, cancellationToken);

                return Result.Success();
            }

        }
    }
}
