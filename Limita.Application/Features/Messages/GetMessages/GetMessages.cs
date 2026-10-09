using Limita.Application.Common.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Limita.Application.Features.Messages.GetMessages;

public sealed record GetMessagesQuery : IRequest<IReadOnlyList<MessageThreadDto>>;
public sealed record MessageThreadDto(Guid Id, string Subject, string Status, DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt, string? LastMessage, DateTimeOffset? LastMessageAt);

public sealed class GetMessagesHandler(IApplicationDbContext db, ICurrentUser currentUser)
    : IRequestHandler<GetMessagesQuery, IReadOnlyList<MessageThreadDto>>
{
    public async Task<IReadOnlyList<MessageThreadDto>> Handle(GetMessagesQuery request, CancellationToken ct)
    {
        if (currentUser.UserId is not Guid userId) throw new UnauthorizedAccessException();
        var threads = await db.MessageThreads.AsNoTracking().Where(x => x.UserId == userId)
            .OrderByDescending(x => x.UpdatedAt)
            .Select(x => new { x.Id, x.Subject, x.Status, x.CreatedAt, x.UpdatedAt }).ToListAsync(ct);
        var ids = threads.Select(x => x.Id).ToArray();
        var lastMessages = await db.Messages.AsNoTracking().Where(x => ids.Contains(x.ThreadId))
            .GroupBy(x => x.ThreadId)
            .Select(g => new { ThreadId = g.Key, Last = g.OrderByDescending(x => x.SentAt).FirstOrDefault() })
            .ToListAsync(ct);
        var lookup = lastMessages.Where(x => x.Last != null).ToDictionary(x => x.ThreadId, x => x.Last!);
        return threads.Select(x =>
        {
            lookup.TryGetValue(x.Id, out var lastMessage);
            return new MessageThreadDto(x.Id, x.Subject, x.Status, x.CreatedAt, x.UpdatedAt,
                lastMessage?.Body, lastMessage?.SentAt);
        }).ToList();
    }
}
