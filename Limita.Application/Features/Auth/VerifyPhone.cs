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
    /// <summary>Confirms the phone number with the code that was sent by SMS. Wrong guesses are counted and saved (ICommitOnFailure).</summary>
    public static class VerifyPhone
    {
        public sealed record Command(string PhoneNumber, string Code) : ICommand, ICommitOnFailure;

        internal sealed class Validator : AbstractValidator<Command>
        {
            public Validator()
            {
                RuleFor(x => x.PhoneNumber).ValidPhoneNumber();
                RuleFor(x => x.Code).ValidOtpCode();
            }
        }

        internal sealed class Handler(IApplicationDbContext db, OtpService otp) : ICommandHandler<Command>
        {
            public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
            {
                var phone = AuthInput.NormalizePhone(request.PhoneNumber);
                var user = await db.Users.FirstOrDefaultAsync(u => u.PhoneNumber == phone, cancellationToken);

                // Same error whether the number is unknown or the code is wrong.
                if (user is null)
                    return Result.Failure(AuthErrors.InvalidCode);

                if (user.IsPhoneVerified)
                    return Result.Success();

                var checkedCode = await otp.VerifyAsync(user, OtpPurpose.PhoneVerification, request.Code, cancellationToken);
                if (checkedCode.IsFailure)
                    return checkedCode;

                user.VerifyPhone();
                return Result.Success();
            }
        }
    }
}
