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
    /// Signs a user in with email and password and returns an access token plus a refresh token.
    /// Five wrong passwords lock the account for 15 minutes. Failed attempts are saved (ICommitOnFailure).
    /// </summary>
    public static class Login
    {
        public sealed record Command(string Email, string Password) : ICommand<Response>, ICommitOnFailure;

        public sealed record Response(AuthTokens Tokens, UserProfile User);

        internal sealed class Validator : AbstractValidator<Command>
        {
            public Validator()
            {
                RuleFor(x => x.Email).NotEmpty().MaximumLength(256);
                RuleFor(x => x.Password).NotEmpty().MaximumLength(128);
            }
        }

        internal sealed class Handler(IApplicationDbContext db, IPasswordHasher hasher, TokenIssuer issuer, TimeProvider time)
            : ICommandHandler<Command, Response>
        {
            public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
            {
                var email = AuthInput.NormalizeEmail(request.Email);
                var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

                if (user is null)
                {
                    hasher.SimulateVerification(request.Password);
                    return AuthErrors.InvalidCredentials;
                }

                var now = time.GetUtcNow();

                if (user.IsLockedOut(now))
                    return AuthErrors.AccountLocked;

                if (!hasher.Verify(request.Password, user.PasswordHash))
                {
                    user.RegisterFailedLogin(now);
                    return AuthErrors.InvalidCredentials;
                }

                // Checked after the password, so only the real owner learns the phone is unverified.
                if (!user.IsPhoneVerified)
                    return AuthErrors.PhoneNotVerified;

                user.RegisterSuccessfulLogin();

                return new Response(issuer.Issue(user), UserProfile.From(user));
            }
        }
    }
}
