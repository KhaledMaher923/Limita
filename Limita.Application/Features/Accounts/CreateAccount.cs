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
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Limita.Application.Features.Accounts
{
    public static class CreateAccount
    {
        public sealed record Command(AccountType Type, string Currency) : ICommand<Response>;

        public sealed record Response(
            Guid Id,
            string AccountNumber,
            AccountType Type,
            string Currency,
            decimal Balance,
            AccountStatus Status);

        internal sealed class Validator : AbstractValidator<Command>
        {
            public Validator()
            {
                RuleFor(x => x.Type).IsInEnum();
                RuleFor(x => x.Currency).NotEmpty().Length(3).Matches("^[A-Za-z]{3}$");
            }
        }

        internal sealed class Handler(IApplicationDbContext db, ICurrentUser currentUser)
            : ICommandHandler<Command, Response>
        {
            public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
            {
                if (currentUser.UserId is not { } userId)
                    return Error.Unauthorized("Auth.Unauthenticated", "You must be signed in.");

                var accountNumber = await GenerateUniqueAccountNumberAsync(cancellationToken);
                var account = Account.Open(userId, accountNumber, request.Type, request.Currency);

                db.Accounts.Add(account); // TransactionBehavior saves and commits

                return new Response(
                    account.Id,
                    account.AccountNumber,
                    account.Type,
                    account.Balance.Currency,
                    account.Balance.Amount,
                    account.Status);
            }

            private async Task<string> GenerateUniqueAccountNumberAsync(CancellationToken cancellationToken)
            {
                for (var attempt = 0; attempt < 5; attempt++)
                {
                    var number = string.Concat(Enumerable.Range(0, 12).Select(_ => RandomNumberGenerator.GetInt32(0, 10)));

                    if (!await db.Accounts.AnyAsync(a => a.AccountNumber == number, cancellationToken))
                        return number;
                }

                throw new InvalidOperationException("Could not generate a unique account number.");
            }


        }
    }
}