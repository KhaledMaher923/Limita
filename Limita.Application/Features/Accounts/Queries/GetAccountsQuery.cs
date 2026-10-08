using Limita.Application.Common;
using Limita.Application.Common.Abstractions;
using Limita.Application.Features.Accounts;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Limita.Application.Features.Accounts.Queries;

public sealed record GetAccountsQuery : IRequest<Result<List<AccountDto>>>;

internal sealed class GetAccountsQueryHandler : IRequestHandler<GetAccountsQuery, Result<List<AccountDto>>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetAccountsQueryHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<List<AccountDto>>> Handle(GetAccountsQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? throw new UnauthorizedAccessException();

        var accounts = await _db.Accounts
            .Where(a => a.UserId == userId)
            .Select(a => new AccountDto(
                a.Id,
                a.AccountNumber,
                a.Type.ToString(),
                a.Status.ToString(),
                a.Balance.Amount,
                a.Balance.Currency))
            .ToListAsync(cancellationToken);

        return Result.Success(accounts);
    }
}