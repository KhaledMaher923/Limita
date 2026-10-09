using Limita.Application.Common.Abstractions;
using Limita.Application.Features.Notifications;
using Limita.Domain.Entities;
using Limita.Domain.Enums;

namespace Limita.Infrastructure.Messaging;

/// <summary>Adds a notification to the current EF unit of work. The caller's command transaction persists it.</summary>
public sealed class NotificationPublisher(IApplicationDbContext db, TimeProvider timeProvider) : INotificationPublisher
{
    public Task PublishAsync(Guid userId, NotificationType type, string title, string body,
        Guid? relatedEntityId = null, CancellationToken cancellationToken = default)
    {
        db.Notifications.Add(Notification.Create(userId, type, title, body, relatedEntityId, timeProvider.GetUtcNow()));
        return Task.CompletedTask;
    }
}
