using Limita.Application.Common;
using Limita.Application.Common.Abstractions;
using Limita.Application.Features.Home;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Limita.Application.Features.Home.Queries;

public sealed record GetHomeDashboardQuery : IRequest<Result<HomeDashboardDto>>;

internal sealed class GetHomeDashboardQueryHandler : IRequestHandler<GetHomeDashboardQuery, Result<HomeDashboardDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetHomeDashboardQueryHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<HomeDashboardDto>> Handle(GetHomeDashboardQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? throw new UnauthorizedAccessException();

        var accounts = await _db.Accounts
            .Where(a => a.UserId == userId)
            .ToListAsync(cancellationToken);

        var totalBalance = accounts.Sum(a => a.Balance.Amount);
        var currency = accounts.FirstOrDefault()?.Balance.Currency ?? "USD";

        var accountSummaries = accounts.Select(a => new AccountSummaryDto(
            a.Id,
            a.AccountNumber,
            a.Type.ToString(),
            a.Balance.Amount,
            a.Balance.Currency)).ToList();

        var userAccountIds = accounts.Select(a => a.Id).ToList();

        var cards = await _db.Cards
            .Where(c => userAccountIds.Contains(c.AccountId))
            .Select(c => new CardSummaryDto(
                c.Id,
                c.Last4,
                c.Network.ToString(),
                c.Status.ToString()))
            .ToListAsync(cancellationToken);

        var recentTransactions = await _db.Transactions
            .Where(t => userAccountIds.Contains(t.AccountId))
            .OrderByDescending(t => t.CreatedAt)
            .Take(10)
            .Select(t => new RecentTransactionDto(
                t.Id,
                t.AccountId,
                t.CreatedAt,
                t.Description ?? string.Empty,
                t.Amount.Amount,
                t.Type.ToString(),
                t.Category.ToString()))
            .ToListAsync(cancellationToken);

        var dashboard = new HomeDashboardDto(
            totalBalance,
            currency,
            accountSummaries,
            cards,
            recentTransactions);

        return Result.Success(dashboard);
    }
}