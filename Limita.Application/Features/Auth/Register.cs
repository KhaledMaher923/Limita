using FluentValidation;
using Limita.Application.Common;
using Limita.Application.Common.Abstractions;
using Limita.Application.Common.Messaging;
using Limita.Domain.Entities;
using Limita.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Limita.Application.Features.Auth
{
    /// <summary>Creates an unverified user and sends a verification code to the phone. The user cannot sign in until the phone is verified.</summary>
    public static class Register
    {
        public sealed record Command(string FullName, string Email, string PhoneNumber, string Password) : ICommand<Response>;

        public sealed record Response(Guid UserId, string PhoneNumber, string Message);

        internal sealed class Validator : AbstractValidator<Command>
        {
            public Validator()
            {
                RuleFor(x => x.FullName).NotEmpty().MaximumLength(100);
                RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
                RuleFor(x => x.PhoneNumber).ValidPhoneNumber();
                RuleFor(x => x.Password).StrongPassword();
            }
        }

        internal sealed class Handler(
            IApplicationDbContext db,
            IPasswordHasher hasher,
            OtpService otp,
            TimeProvider time) : ICommandHandler<Command, Response>
        {
            public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
            {
                var email = AuthInput.NormalizeEmail(request.Email);
                var phone = AuthInput.NormalizePhone(request.PhoneNumber);

                if (await db.Users.AnyAsync(u => u.Email == email, cancellationToken))
                    return AuthErrors.EmailTaken;

                if (await db.Users.AnyAsync(u => u.PhoneNumber == phone, cancellationToken))
                    return AuthErrors.PhoneTaken;

                var user = User.Register(request.FullName, email, phone, hasher.Hash(request.Password), time.GetUtcNow());
                db.Users.Add(user);

                var sent = await otp.IssueAsync(user, OtpPurpose.PhoneVerification, cancellationToken);
                if (sent.IsFailure)
                    return sent.Error;

                return new Response(user.Id, user.PhoneNumber, "A verification code was sent to your phone.");
            }
        }
    }
}
