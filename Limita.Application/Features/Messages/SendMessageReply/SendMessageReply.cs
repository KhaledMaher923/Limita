using FluentValidation;
using Limita.Application.Common;
using Limita.Application.Common.Abstractions;
using Limita.Application.Common.Messaging;
using Limita.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Limita.Application.Features.Messages.SendMessageReply;

public sealed record SendMessageReplyCommand(Guid ThreadId, string Body) : ICommand<Guid>;

public sealed class SendMessageReplyHandler(IApplicationDbContext db, ICurrentUser currentUser, TimeProvider timeProvider)
    : ICommandHandler<SendMessageReplyCommand, Guid>
{
    public async Task<Result<Guid>> Handle(SendMessageReplyCommand request, CancellationToken ct)
    {
        if (currentUser.UserId is not Guid userId) throw new UnauthorizedAccessException();
        var thread = await db.MessageThreads.FirstOrDefaultAsync(x => x.Id == request.ThreadId && x.UserId == userId, ct);
        if (thread is null) return Result.Failure<Guid>(Error.NotFound("Messages.ThreadNotFound", "Message thread was not found."));
        if (thread.Status != "Open") return Result.Failure<Guid>(Error.Conflict("Messages.ThreadClosed", "This message thread is closed."));
        var message = Message.Send(thread.Id, userId, request.Body, isFromBank: false, timeProvider.GetUtcNow());
        thread.Touch(timeProvider.GetUtcNow());
        db.Messages.Add(message);
        return Result.Success(message.Id);
    }
}
public sealed class SendMessageReplyCommandValidator : FluentValidation.AbstractValidator<SendMessageReplyCommand>
{
    public SendMessageReplyCommandValidator() => RuleFor(x => x.Body).NotEmpty().MaximumLength(2000);
}

