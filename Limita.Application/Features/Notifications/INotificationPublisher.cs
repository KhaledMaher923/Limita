using Limita.Domain.Enums;

namespace Limita.Application.Features.Notifications;

/// <summary>Contract for modules that need to enqueue an in-app notification.</summary>
public interface INotificationPublisher
{
    Task PublishAsync(Guid userId, NotificationType type, string title, string body,
        Guid? relatedEntityId = null, CancellationToken cancellationToken = default);
}
