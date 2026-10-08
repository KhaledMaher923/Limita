using Limita.Application.Common;
using Limita.Application.Common.Abstractions;
using Limita.Application.Common.Messaging;
using Limita.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Limita.Application.Features.SpendingLimits;

public sealed record GetSpendingLimitsQuery
    : IQuery<IReadOnlyList<SpendingLimitDto>>;

public sealed record SpendingLimitDto(
    Guid Id,
    Guid AccountId,
    decimal LimitAmount,
    decimal SpentSoFar,
    string Currency,
    decimal PercentUsed,
    bool IsExceeded,
    string? Category,
    string Period);

internal sealed class GetSpendingLimitsHandler(
    IApplicationDbContext dbContext,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
    : IQueryHandler<GetSpendingLimitsQuery, IReadOnlyList<SpendingLimitDto>>
{
    public async Task<Result<IReadOnlyList<SpendingLimitDto>>> Handle(
        GetSpendingLimitsQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not Guid userId)
        {
            return Result.Failure<IReadOnlyList<SpendingLimitDto>>(
                Error.Unauthorized("Auth.Required", "Sign-in is required."));
        }

        var limits = await dbContext.SpendingLimits
            .AsNoTracking()
            .Where(limit => limit.UserId == userId)
            .ToListAsync(cancellationToken);

        var now = timeProvider.GetUtcNow();
        var response = new List<SpendingLimitDto>();

        foreach (var limit in limits)
        {
            if (limit.AccountId is not Guid accountId)
                continue;

            var periodStart = limit.GetPeriodStart(now);

            var query = dbContext.Transactions.Where(transaction =>
                transaction.AccountId == accountId
                && transaction.Type == TransactionType.Debit
                && transaction.CreatedAt >= periodStart);

            if (limit.Category is not null)
            {
                query = query.Where(transaction =>
                    transaction.Category == limit.Category);
            }

            var spent = await query
                .Select(transaction =>
                    (decimal?)transaction.Amount.Amount)
                .SumAsync(cancellationToken) ?? 0m;

            var percent = limit.LimitAmount.Amount == 0m
                ? 0m
                : decimal.Round(
                    spent / limit.LimitAmount.Amount * 100m,
                    2);

            response.Add(new SpendingLimitDto(
                limit.Id,
                accountId,
                limit.LimitAmount.Amount,
                spent,
                limit.LimitAmount.Currency,
                percent,
                spent > limit.LimitAmount.Amount,
                limit.Category?.ToString(),
                limit.Period.ToString()));
        }

        return Result.Success<IReadOnlyList<SpendingLimitDto>>(response);
    }
}