using Limita.Application.Common;
using Limita.Application.Common.Abstractions;
using Limita.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Limita.Application.Features.Cards.Commands;

public sealed record DeleteCardCommand(Guid CardId) : IRequest<Result>;

internal sealed class DeleteCardCommandHandler : IRequestHandler<DeleteCardCommand, Result>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public DeleteCardCommandHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(DeleteCardCommand command, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? throw new UnauthorizedAccessException();

        var card = await _db.Cards
            .Where(c => c.Id == command.CardId && _db.Accounts.Any(a => a.Id == c.AccountId && a.UserId == userId))
            .FirstOrDefaultAsync(cancellationToken);

        if (card is null)
            return Result.Failure(Error.NotFound("Card.NotFound", "Card not found."));

        card.Close();
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}