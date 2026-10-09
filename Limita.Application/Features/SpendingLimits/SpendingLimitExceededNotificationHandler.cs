using Limita.Application.Common.Abstractions;
using Limita.Application.Common.DomainEvents;
using Limita.Domain.Entities;
using Limita.Domain.Enums;
using Limita.Domain.Events;
using MediatR;

namespace Limita.Application.Features.SpendingLimits;

internal sealed class SpendingLimitExceededNotificationHandler(
    IApplicationDbContext dbContext,
    TimeProvider timeProvider)
    : INotificationHandler<
        DomainEventNotification<SpendingLimitExceededEvent>>
{
    public Task Handle(
        DomainEventNotification<SpendingLimitExceededEvent> notification,
        CancellationToken cancellationToken)
    {
        var spendingLimitEvent = notification.DomainEvent;

        var savedNotification = Notification.Create(
            spendingLimitEvent.UserId,
            NotificationType.SpendingLimitExceeded,
            "Spending limit exceeded",
            $"You have spent {spendingLimitEvent.SpentSoFar}, which is above your spending limit {spendingLimitEvent.Limit}.",
            spendingLimitEvent.SpendingLimitId,
            timeProvider.GetUtcNow());

        dbContext.Notifications.Add(savedNotification);

        return Task.CompletedTask;
    }
}