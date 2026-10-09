using Limita.Application.Common.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Limita.Application.Features.Transactions.GetTransactionReport;

public sealed record GetTransactionReportQuery(int Year, int Month) : IRequest<IReadOnlyList<TransactionReportItem>>;
public sealed record TransactionReportItem(Guid Id, DateTimeOffset CreatedAt, string Type, decimal Amount,
    string Currency, decimal BalanceAfter, string Category, string? Description);

public sealed class GetTransactionReportHandler(IApplicationDbContext db, ICurrentUser currentUser)
    : IRequestHandler<GetTransactionReportQuery, IReadOnlyList<TransactionReportItem>>
{
    public async Task<IReadOnlyList<TransactionReportItem>> Handle(GetTransactionReportQuery request, CancellationToken ct)
    {
        if (currentUser.UserId is not Guid userId) throw new UnauthorizedAccessException();
        if (request.Month is < 1 or > 12 || request.Year is < 2000 or > 2200) throw new ArgumentOutOfRangeException(nameof(request.Month), "Month must be 1-12 and year must be valid.");
        var start = new DateTimeOffset(request.Year, request.Month, 1, 0, 0, 0, TimeSpan.Zero);
        var end = start.AddMonths(1);
        return await (from transaction in db.Transactions.AsNoTracking()
                      join account in db.Accounts.AsNoTracking() on transaction.AccountId equals account.Id
                      where account.UserId == userId && transaction.CreatedAt >= start && transaction.CreatedAt < end
                      orderby transaction.CreatedAt descending
                      select new TransactionReportItem(transaction.Id, transaction.CreatedAt, transaction.Type.ToString(),
                          transaction.Amount.Amount, transaction.Amount.Currency, transaction.BalanceAfter,
                          transaction.Category.ToString(), transaction.Description))
            .ToListAsync(ct);
    }
}
