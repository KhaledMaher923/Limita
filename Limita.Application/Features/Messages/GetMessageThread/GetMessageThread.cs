using Limita.Application.Common.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Limita.Application.Features.Messages.GetMessageThread;

public sealed record GetMessageThreadQuery(Guid ThreadId) : IRequest<MessageThreadDetailsDto?>;
public sealed record MessageDto(Guid Id, Guid SenderUserId, string Body, bool IsFromBank, DateTimeOffset SentAt);
public sealed record MessageThreadDetailsDto(Guid Id, string Subject, string Status, DateTimeOffset CreatedAt,
    IReadOnlyList<MessageDto> Messages);

public sealed class GetMessageThreadHandler(IApplicationDbContext db, ICurrentUser currentUser)
    : IRequestHandler<GetMessageThreadQuery, MessageThreadDetailsDto?>
{
    public async Task<MessageThreadDetailsDto?> Handle(GetMessageThreadQuery request, CancellationToken ct)
    {
        if (currentUser.UserId is not Guid userId) throw new UnauthorizedAccessException();
        var thread = await db.MessageThreads.AsNoTracking().FirstOrDefaultAsync(x => x.Id == request.ThreadId && x.UserId == userId, ct);
        if (thread is null) return null;
        var messages = await db.Messages.AsNoTracking().Where(x => x.ThreadId == thread.Id)
            .OrderBy(x => x.SentAt).Select(x => new MessageDto(x.Id, x.SenderUserId, x.Body, x.IsFromBank, x.SentAt)).ToListAsync(ct);
        return new MessageThreadDetailsDto(thread.Id, thread.Subject, thread.Status, thread.CreatedAt, messages);
    }
}
