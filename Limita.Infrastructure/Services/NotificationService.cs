using Limita.Application.Common.Abstractions;
using Limita.Application.Common.DomainEvents;
using Limita.Domain.Entities;
using Limita.Domain.Enums;
using Limita.Domain.Events;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Limita.Infrastructure.Services;

internal sealed class CardAddedNotificationHandler : INotificationHandler<DomainEventNotification<CardAddedEvent>>
{
    private readonly IApplicationDbContext _db;
    private readonly TimeProvider _timeProvider;

    public CardAddedNotificationHandler(IApplicationDbContext db, TimeProvider timeProvider)
    {
        _db = db;
        _timeProvider = timeProvider;
    }

    public async Task Handle(DomainEventNotification<CardAddedEvent> notification, CancellationToken cancellationToken)
    {
        var cardAddedEvent = notification.DomainEvent;

        var card = await _db.Cards
            .FirstOrDefaultAsync(c => c.Id == cardAddedEvent.CardId, cancellationToken);

        if (card is null)
            return;

        var account = await _db.Accounts
            .FirstOrDefaultAsync(a => a.Id == card.AccountId, cancellationToken);

        if (account is null)
            return;

        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == account.UserId, cancellationToken);

        if (user is null)
            return;

        var now = _timeProvider.GetUtcNow();

        var notificationEntity = Notification.Create(
            user.Id,
            NotificationType.CardAdded,
            "New Card Added",
            $"A new {card.Network} card ending in {card.Last4} has been added to your account.",
            card.Id,
            now);

        _db.Notifications.Add(notificationEntity);
        await _db.SaveChangesAsync(cancellationToken);
    }
}