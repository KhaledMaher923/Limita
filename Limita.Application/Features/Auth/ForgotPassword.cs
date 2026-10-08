using FluentValidation;
using Limita.Application.Common.Abstractions;
using Limita.Application.Common.Messaging;
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

        //internal sealed class Handler(IApplicationDbContext db, Otp)


    }
}
