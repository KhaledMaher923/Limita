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

namespace Limita.Application.Features.Accounts
{
    public static class GetAccounts
    {
        public sealed record Query : IQuery<IReadOnlyList<AccountSummary>>;

        public sealed record AccountSummary(
            Guid Id,
            string AccountNumber,
            AccountType Type,
            string Currency,
            decimal Balance,
            AccountStatus Status);

        internal sealed class Handler(IApplicationDbContext db, ICurrentUser currentUser)
            : IQueryHandler<Query, IReadOnlyList<AccountSummary>>
        {
            public async Task<Result<IReadOnlyList<AccountSummary>>> Handle(Query request, CancellationToken cancellationToken)
            {
                if (currentUser.UserId is not { } userId)
                    return Error.Unauthorized("Auth.Unauthenticated", "You must be signed in.");

                var accounts = await db.Accounts
                    .AsNoTracking()
                    .Where(a => a.UserId == userId)
                    .OrderBy(a => a.AccountNumber)
                    .Select(a => new AccountSummary(
                        a.Id,
                        a.AccountNumber,
                        a.Type,
                        a.Balance.Currency,
                        a.Balance.Amount,
                        a.Status))
                    .ToListAsync(cancellationToken);

                return Result.Success<IReadOnlyList<AccountSummary>>(accounts);
            }
        }
    }
}
