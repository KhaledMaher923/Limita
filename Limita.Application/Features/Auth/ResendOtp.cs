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
    /// <summary>Sends a new verification code (at most one per minute). Unknown or already verified numbers get a silent success.</summary>
    public static class ResendOtp
    {
        public sealed record Command(string PhoneNumber) : ICommand;

        internal sealed class Validator : AbstractValidator<Command>
        {
            public Validator() => RuleFor(x => x.PhoneNumber).ValidPhoneNumber();
        }

        internal sealed class Handler(IApplicationDbContext db, OtpService otp) : ICommandHandler<Command>
        {
            public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
            {
                var phone = AuthInput.NormalizePhone(request.PhoneNumber);
                var user = await db.Users.FirstOrDefaultAsync(u => u.PhoneNumber == phone, cancellationToken);

                if (user is null || user.IsPhoneVerified)
                    return Result.Success();

                return await otp.IssueAsync(user, OtpPurpose.PhoneVerification, cancellationToken);
            }
        }

    }
}
