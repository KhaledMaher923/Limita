using Limita.Application.Common.Abstractions;
using Limita.Application.Common.DomainEvents;
using Limita.Domain.Entities;
using Limita.Domain.Enums;
using Limita.Domain.Events;
using MediatR;

namespace Limita.Application.Features.SpendingLimits;

internal sealed class SpendingLimitHalfwayReachedNotificationHandler(
    IApplicationDbContext dbContext,
    TimeProvider timeProvider)
    : INotificationHandler<
        DomainEventNotification<SpendingLimitHalfwayReachedEvent>>
{
    public Task Handle(
        DomainEventNotification<SpendingLimitHalfwayReachedEvent> notification,
        CancellationToken cancellationToken)
    {
        var spendingLimitEvent = notification.DomainEvent;

        var savedNotification = Notification.Create(
            spendingLimitEvent.UserId,
            NotificationType.SpendingLimitHalfwayReached,
            "Spending limit halfway reached",
            $"You have spent {spendingLimitEvent.SpentSoFar} of your spending limit {spendingLimitEvent.Limit}.",
            spendingLimitEvent.SpendingLimitId,
            timeProvider.GetUtcNow());

        dbContext.Notifications.Add(savedNotification);

        return Task.CompletedTask;
    }
}