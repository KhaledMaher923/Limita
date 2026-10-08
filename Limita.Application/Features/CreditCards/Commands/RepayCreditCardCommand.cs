using Limita.Application.Common;
using Limita.Application.Common.Abstractions;
using Limita.Application.Features.CreditCards;
using Limita.Domain.Entities;
using Limita.Domain.Enums;
using Limita.Domain.ValueObjects;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Limita.Application.Features.CreditCards.Commands;

public sealed record RepayCreditCardCommand(RepayCreditCardRequest Request) : IRequest<Result<RepayCreditCardResponse>>;

internal sealed class RepayCreditCardCommandHandler : IRequestHandler<RepayCreditCardCommand, Result<RepayCreditCardResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;

    public RepayCreditCardCommandHandler(IApplicationDbContext db, ICurrentUser currentUser, TimeProvider timeProvider)
    {
        _db = db;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task<Result<RepayCreditCardResponse>> Handle(RepayCreditCardCommand command, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? throw new UnauthorizedAccessException();

        var card = await _db.Cards
            .Where(c => c.Id == command.Request.CardId && _db.Accounts.Any(a => a.Id == c.AccountId && a.UserId == userId))
            .FirstOrDefaultAsync(cancellationToken);

        if (card is null)
            return Result.Failure<RepayCreditCardResponse>(Error.NotFound("Card.NotFound", "Card not found."));

        if (card.Network != CardNetwork.Visa && card.Network != CardNetwork.Mastercard && card.Network != CardNetwork.Meeza)
            return Result.Failure<RepayCreditCardResponse>(Error.BusinessRule("Card.NotCreditCard", "This card is not a credit card."));

        var fromAccount = await _db.Accounts
            .FirstOrDefaultAsync(a => a.Id == command.Request.FromAccountId && a.UserId == userId, cancellationToken);

        if (fromAccount is null)
            return Result.Failure<RepayCreditCardResponse>(Error.NotFound("Account.NotFound", "Source account not found."));

        if (fromAccount.Status != AccountStatus.Active)
            return Result.Failure<RepayCreditCardResponse>(Error.BusinessRule("Account.NotActive", "Source account is not active."));

        var cardAccount = await _db.Accounts.FirstOrDefaultAsync(a => a.Id == card.AccountId, cancellationToken);
        if (cardAccount is null)
            return Result.Failure<RepayCreditCardResponse>(Error.NotFound("Account.NotFound", "Card account not found."));

        var repaymentAmount = Money.Of(command.Request.Amount, cardAccount.Balance.Currency);

        if (repaymentAmount.Amount <= 0)
            return Result.Failure<RepayCreditCardResponse>(Error.Validation("Amount.Invalid", "Repayment amount must be positive."));

        var now = _timeProvider.GetUtcNow();

        if (!fromAccount.HasSufficientFunds(repaymentAmount))
            return Result.Failure<RepayCreditCardResponse>(Error.BusinessRule("InsufficientFunds", "Insufficient funds in source account."));

        await _db.BeginTransactionAsync(cancellationToken);

        try
        {
            var debitTransaction = fromAccount.Debit(repaymentAmount, TransactionCategory.General, $"Credit card repayment: {card.Last4}", now);
            var creditTransaction = cardAccount.Credit(repaymentAmount, TransactionCategory.General, $"Credit card repayment: {card.Last4}", now);

            _db.Transactions.Add(debitTransaction);
            _db.Transactions.Add(creditTransaction);

            await _db.SaveChangesAsync(cancellationToken);
            await _db.CommitTransactionAsync(cancellationToken);

            return Result.Success(new RepayCreditCardResponse(creditTransaction.Id, cardAccount.Balance.Amount));
        }
        catch
        {
            await _db.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
}