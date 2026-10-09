using FluentValidation;
using Limita.Application.Common;
using Limita.Application.Common.Abstractions;
using Limita.Application.Common.Messaging;
using Limita.Domain.Entities;
using MediatR;

namespace Limita.Application.Features.Messages.StartMessageThread;

public sealed record StartMessageThreadCommand(string Subject, string Body) : ICommand<Guid>;

public sealed class StartMessageThreadHandler(IApplicationDbContext db, ICurrentUser currentUser, TimeProvider timeProvider)
    : ICommandHandler<StartMessageThreadCommand, Guid>
{
    public Task<Result<Guid>> Handle(StartMessageThreadCommand request, CancellationToken ct)
    {
        if (currentUser.UserId is not Guid userId) throw new UnauthorizedAccessException();
        var now = timeProvider.GetUtcNow();
        var thread = MessageThread.Open(userId, request.Subject, now);
        var message = Message.Send(thread.Id, userId, request.Body, isFromBank: false, now);
        db.MessageThreads.Add(thread);
        db.Messages.Add(message);
        return Task.FromResult(Result.Success(thread.Id));
    }
}
public sealed class StartMessageThreadCommandValidator : FluentValidation.AbstractValidator<StartMessageThreadCommand>
{
    public StartMessageThreadCommandValidator()
    {
        RuleFor(x => x.Subject).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Body).NotEmpty().MaximumLength(2000);
    }
}

