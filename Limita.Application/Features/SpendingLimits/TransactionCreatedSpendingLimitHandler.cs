using Limita.Application.Common.Abstractions;
using Limita.Application.Common.DomainEvents;
using Limita.Domain.Enums;
using Limita.Domain.Events;
using Limita.Domain.ValueObjects;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Limita.Application.Features.SpendingLimits;

internal sealed class TransactionCreatedSpendingLimitHandler(
    IApplicationDbContext dbContext,
    TimeProvider timeProvider)
    : INotificationHandler<
        DomainEventNotification<TransactionCreatedEvent>>
{
    public async Task Handle(
        DomainEventNotification<TransactionCreatedEvent> notification,
        CancellationToken cancellationToken)
    {
        var created = notification.DomainEvent;

        if (created.Type != TransactionType.Debit)
            return;

        if (created.Category is TransactionCategory.Transfer
            or TransactionCategory.Savings
            or TransactionCategory.Exchange)
            return;

        var now = timeProvider.GetUtcNow();

        var limits = await dbContext.SpendingLimits
            .Where(limit =>
                limit.IsActive
                && limit.AccountId == created.AccountId
                && (limit.Category == null
                    || limit.Category == created.Category))
            .ToListAsync(cancellationToken);

        foreach (var limit in limits)
        {
            var periodStart = limit.GetPeriodStart(now);

            var query = dbContext.Transactions.Where(transaction =>
                transaction.AccountId == created.AccountId
                && transaction.Type == TransactionType.Debit
                && transaction.CreatedAt >= periodStart);

            if (limit.Category is not null)
            {
                query = query.Where(transaction =>
                    transaction.Category == limit.Category);
            }

            var previousSpend = await query
                .Select(transaction =>
                    (decimal?)transaction.Amount.Amount)
                .SumAsync(cancellationToken) ?? 0m;

            var totalSpend = Money.Of(
                previousSpend + created.Amount.Amount,
                created.Amount.Currency);

            limit.EvaluateSpending(totalSpend, now);
        }
    }
}
