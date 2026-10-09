using Limita.Application.Common.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Limita.Application.Features.Notifications.GetNotifications;

public sealed record GetNotificationsQuery(bool UnreadOnly = false) : IRequest<IReadOnlyList<NotificationDto>>;
public sealed record NotificationDto(Guid Id, string Type, string Title, string Body, bool IsRead,
    Guid? RelatedEntityId, DateTimeOffset CreatedAt);

public sealed class GetNotificationsHandler(IApplicationDbContext db, ICurrentUser currentUser)
    : IRequestHandler<GetNotificationsQuery, IReadOnlyList<NotificationDto>>
{
    public async Task<IReadOnlyList<NotificationDto>> Handle(GetNotificationsQuery request, CancellationToken ct)
    {
        if (currentUser.UserId is not Guid userId) throw new UnauthorizedAccessException();
        var query = db.Notifications.AsNoTracking().Where(x => x.UserId == userId);
        if (request.UnreadOnly) query = query.Where(x => !x.IsRead);
        return await query.OrderByDescending(x => x.CreatedAt)
            .Select(x => new NotificationDto(x.Id, x.Type.ToString(), x.Title, x.Body, x.IsRead, x.RelatedEntityId, x.CreatedAt))
            .ToListAsync(ct);
    }
}
