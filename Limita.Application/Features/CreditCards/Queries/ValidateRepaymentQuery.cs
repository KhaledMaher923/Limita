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

public sealed record ValidateRepaymentQuery(ValidateRepaymentRequest Request) : IRequest<Result<ValidateRepaymentResponse>>;

internal sealed class ValidateRepaymentQueryHandler : IRequestHandler<ValidateRepaymentQuery, Result<ValidateRepaymentResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public ValidateRepaymentQueryHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<ValidateRepaymentResponse>> Handle(ValidateRepaymentQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? throw new UnauthorizedAccessException();

        var card = await _db.Cards
            .Where(c => c.Id == request.Request.CardId && _db.Accounts.Any(a => a.Id == c.AccountId && a.UserId == userId))
            .FirstOrDefaultAsync(cancellationToken);

        if (card is null)
            return Result.Failure<ValidateRepaymentResponse>(Error.NotFound("Card.NotFound", "Card not found."));

        if (card.Network != CardNetwork.Visa && card.Network != CardNetwork.Mastercard && card.Network != CardNetwork.Meeza)
            return Result.Failure<ValidateRepaymentResponse>(Error.BusinessRule("Card.NotCreditCard", "This card is not a credit card."));

        var account = await _db.Accounts.FirstOrDefaultAsync(a => a.Id == card.AccountId, cancellationToken);
        if (account is null)
            return Result.Failure<ValidateRepaymentResponse>(Error.NotFound("Account.NotFound", "Account not found."));

        var currentBalance = account.Balance.Amount;
        var minimumPayment = Math.Max(50m, Math.Round(currentBalance * 0.05m, 2));

        if (request.Request.Amount <= 0)
            return Result.Success(new ValidateRepaymentResponse(false, "Repayment amount must be positive.", minimumPayment, currentBalance));

        if (request.Request.Amount < minimumPayment)
            return Result.Success(new ValidateRepaymentResponse(false, $"Repayment amount must be at least the minimum payment of {minimumPayment:0.00}.", minimumPayment, currentBalance));

        if (request.Request.Amount > currentBalance)
            return Result.Success(new ValidateRepaymentResponse(false, $"Repayment amount cannot exceed the current balance of {currentBalance:0.00}.", minimumPayment, currentBalance));

        return Result.Success(new ValidateRepaymentResponse(true, null, minimumPayment, currentBalance));
    }
}