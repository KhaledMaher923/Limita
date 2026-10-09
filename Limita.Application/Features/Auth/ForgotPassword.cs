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
    /// <summary>Sends a password reset code by SMS. The answer is always success, so nobody can discover which numbers are registered.</summary>
    public static class ForgotPassword
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

                if (user is { IsPhoneVerified: true })
                {
                    // A cooldown failure is ignored on purpose: the caller must not learn whether the number exists.
                    await otp.IssueAsync(user, OtpPurpose.PasswordReset, cancellationToken);
                }

                return Result.Success();
            }
        }
    }
}
