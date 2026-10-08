using Limita.Application.Common;
using Limita.Application.Common.Abstractions;
using Limita.Application.Features.Cards;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Limita.Application.Features.Cards.Queries;

public sealed record GetCardByIdQuery(Guid CardId) : IRequest<Result<CardDto>>;

internal sealed class GetCardByIdQueryHandler : IRequestHandler<GetCardByIdQuery, Result<CardDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetCardByIdQueryHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<CardDto>> Handle(GetCardByIdQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? throw new UnauthorizedAccessException();

        var card = await _db.Cards
            .Where(c => c.Id == request.CardId && _db.Accounts.Any(a => a.Id == c.AccountId && a.UserId == userId))
            .Select(c => new CardDto(
                c.Id,
                c.AccountId,
                c.Network.ToString(),
                c.Last4,
                c.ExpiryMonth,
                c.ExpiryYear,
                c.HolderName,
                c.Status.ToString(),
                c.IsExpired(DateTimeOffset.UtcNow)))
            .FirstOrDefaultAsync(cancellationToken);

        if (card is null)
            return Result.Failure<CardDto>(Error.NotFound("Card.NotFound", "Card not found."));

        return Result.Success(card);
    }
}