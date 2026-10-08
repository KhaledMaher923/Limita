using Limita.Application.Common;
using Limita.Application.Common.Abstractions;
using Limita.Application.Features.CreditCards;
using Limita.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Limita.Application.Features.CreditCards.Queries;

public sealed record GetCreditCardStatementQuery(Guid CardId) : IRequest<Result<CreditCardStatementDto>>;

internal sealed class GetCreditCardStatementQueryHandler : IRequestHandler<GetCreditCardStatementQuery, Result<CreditCardStatementDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetCreditCardStatementQueryHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<CreditCardStatementDto>> Handle(GetCreditCardStatementQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? throw new UnauthorizedAccessException();

        var card = await _db.Cards
            .Where(c => c.Id == request.CardId && _db.Accounts.Any(a => a.Id == c.AccountId && a.UserId == userId))
            .FirstOrDefaultAsync(cancellationToken);

        if (card is null)
            return Result.Failure<CreditCardStatementDto>(Error.NotFound("Card.NotFound", "Card not found."));

        if (card.Network != CardNetwork.Visa && card.Network != CardNetwork.Mastercard && card.Network != CardNetwork.Meeza)
            return Result.Failure<CreditCardStatementDto>(Error.BusinessRule("Card.NotCreditCard", "This card is not a credit card."));

        var account = await _db.Accounts.FirstOrDefaultAsync(a => a.Id == card.AccountId, cancellationToken);
        if (account is null)
            return Result.Failure<CreditCardStatementDto>(Error.NotFound("Account.NotFound", "Account not found."));

        var now = DateTimeOffset.UtcNow;
        var statementDate = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero).AddMonths(-1);
        var dueDate = statementDate.AddDays(25);

        var transactions = await _db.Transactions
            .Where(t => t.AccountId == card.AccountId && t.CreatedAt >= statementDate && t.CreatedAt <= now)
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new StatementTransactionDto(
                t.Id,
                t.CreatedAt,
                t.Description ?? string.Empty,
                t.Amount.Amount,
                t.Type.ToString()))
            .ToListAsync(cancellationToken);

        var currentBalance = account.Balance.Amount;
        var minimumPayment = Math.Max(50m, Math.Round(currentBalance * 0.05m, 2));

        var statement = new CreditCardStatementDto(
            card.Id,
            card.Last4,
            currentBalance,
            account.Balance.Currency,
            statementDate,
            dueDate,
            minimumPayment,
            transactions);

        return Result.Success(statement);
    }
}