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

public sealed record GetAccountBalanceQuery(Guid AccountId) : IRequest<Result<AccountBalanceDto>>;

internal sealed class GetAccountBalanceQueryHandler : IRequestHandler<GetAccountBalanceQuery, Result<AccountBalanceDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetAccountBalanceQueryHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<AccountBalanceDto>> Handle(GetAccountBalanceQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? throw new UnauthorizedAccessException();

        var account = await _db.Accounts
            .FirstOrDefaultAsync(a => a.Id == request.AccountId && a.UserId == userId, cancellationToken);

        if (account is null)
            return Result.Failure<AccountBalanceDto>(Error.NotFound("Account.NotFound", "Account not found."));

        return Result.Success(new AccountBalanceDto(account.Balance.Amount, account.Balance.Currency));
    }
}