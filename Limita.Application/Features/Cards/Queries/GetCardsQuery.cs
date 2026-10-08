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

public sealed record GetCardsQuery : IRequest<Result<List<CardDto>>>;

internal sealed class GetCardsQueryHandler : IRequestHandler<GetCardsQuery, Result<List<CardDto>>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetCardsQueryHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<List<CardDto>>> Handle(GetCardsQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? throw new UnauthorizedAccessException();

        var cards = await _db.Cards
            .Where(c => c.AccountId == _db.Accounts.Where(a => a.UserId == userId).Select(a => a.Id).FirstOrDefault())
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
            .ToListAsync(cancellationToken);

        return Result.Success(cards);
    }
}