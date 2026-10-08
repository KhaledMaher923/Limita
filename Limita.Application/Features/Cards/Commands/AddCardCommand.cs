using Limita.Application.Common;
using Limita.Application.Common.Abstractions;
using Limita.Application.Features.Cards;
using Limita.Domain.Entities;
using Limita.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Limita.Application.Features.Cards.Commands;

public sealed record AddCardCommand(AddCardRequest Request) : IRequest<Result<AddCardResponse>>;

internal sealed class AddCardCommandHandler : IRequestHandler<AddCardCommand, Result<AddCardResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;

    public AddCardCommandHandler(IApplicationDbContext db, ICurrentUser currentUser, TimeProvider timeProvider)
    {
        _db = db;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task<Result<AddCardResponse>> Handle(AddCardCommand command, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? throw new UnauthorizedAccessException();

        var account = await _db.Accounts
            .FirstOrDefaultAsync(a => a.Id == command.Request.AccountId && a.UserId == userId, cancellationToken);

        if (account is null)
            return Result.Failure<AddCardResponse>(Error.NotFound("Account.NotFound", "Account not found."));

        if (account.Status != AccountStatus.Active)
            return Result.Failure<AddCardResponse>(Error.BusinessRule("Account.NotActive", "Account is not active."));

        var now = _timeProvider.GetUtcNow();

        var card = Card.Add(
            account.Id,
            command.Request.Network,
            command.Request.Last4,
            command.Request.ExpiryMonth,
            command.Request.ExpiryYear,
            command.Request.HolderName,
            command.Request.Token,
            now);

        _db.Cards.Add(card);
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success(new AddCardResponse(card.Id));
    }
}