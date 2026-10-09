using Limita.Application.Common;
using Limita.Application.Common.Abstractions;
using Limita.Application.Common.Messaging;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Limita.Application.Features.Notifications.MarkNotificationRead;

public sealed record MarkNotificationReadCommand(Guid NotificationId) : ICommand<bool>;

public sealed class MarkNotificationReadHandler(IApplicationDbContext db, ICurrentUser currentUser)
    : ICommandHandler<MarkNotificationReadCommand, bool>
{
    public async Task<Result<bool>> Handle(MarkNotificationReadCommand request, CancellationToken ct)
    {
        if (currentUser.UserId is not Guid userId) throw new UnauthorizedAccessException();
        var notification = await db.Notifications.FirstOrDefaultAsync(x => x.Id == request.NotificationId && x.UserId == userId, ct);
        if (notification is null) return Result.Failure<bool>(Error.NotFound("Notifications.NotFound", "Notification was not found."));
        notification.MarkAsRead();
        return Result.Success(true); // TransactionBehavior persists command changes.
    }
}
