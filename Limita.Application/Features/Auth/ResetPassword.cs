using FluentValidation;
using Limita.Application.Common;
using Limita.Application.Common.Abstractions;
using Limita.Application.Common.Messaging;
using Limita.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Limita.Application.Features.Auth
{
    /// <summary>Sets a new password using the code from ForgotPassword, then ends every session of the user.</summary>
    public static class ResetPassword
    {
        public sealed record Command(string PhoneNumber, string Code, string NewPassword) : ICommand, ICommitOnFailure;

        internal sealed class Validator : AbstractValidator<Command>
        {
            public Validator()
            {
                RuleFor(x => x.PhoneNumber).ValidPhoneNumber();
                RuleFor(x => x.Code).ValidOtpCode();
                RuleFor(x => x.NewPassword).StrongPassword();
            }
        }

        internal sealed class Handler(
            IApplicationDbContext db,
            OtpService otp,
            IPasswordHasher hasher,
            TokenIssuer issuer) : ICommandHandler<Command>
        {
            public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
            {
                var phone = AuthInput.NormalizePhone(request.PhoneNumber);
                var user = await db.Users.FirstOrDefaultAsync(u => u.PhoneNumber == phone, cancellationToken);

                if (user is null)
                    return Result.Failure(AuthErrors.InvalidCode);

                var checkedCode = await otp.VerifyAsync(user, OtpPurpose.PasswordReset, request.Code, cancellationToken);
                if (checkedCode.IsFailure)
                    return checkedCode;

                user.ChangePassword(hasher.Hash(request.NewPassword));
                user.ClearLockout(); // the owner proved control of the phone
                await issuer.RevokeAllAsync(user.Id, cancellationToken);

                return Result.Success();
            }
        }

    }
}
