using FluentValidation;
using Limita.Application.Common;
using Limita.Application.Common.Abstractions;
using Limita.Application.Common.Messaging;
using Limita.Domain.Entities;
using Limita.Domain.Enums;
using Limita.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Limita.Application.Features.MobilePrepaid.Commands;

/// <summary>Recharges a prepaid phone from one of the customer's accounts. A retried request with the same IdempotencyKey never charges twice.</summary>
public sealed record TopUpMobileCommand(
    Guid AccountId,
    string PhoneNumber,
    decimal Amount,
    string Currency,
    string IdempotencyKey) : ICommand<TopUpMobileResult>;

public sealed record TopUpMobileResult(
    Guid TopUpId,
    decimal Amount,
    string Currency,
    decimal BalanceAfter,
    DateTimeOffset CreatedAt);

public sealed class TopUpMobileValidator : AbstractValidator<TopUpMobileCommand>
{
    public TopUpMobileValidator()
    {
        RuleFor(command => command.AccountId).NotEmpty();

        RuleFor(command => command.PhoneNumber)
            .NotEmpty()
            .Matches(@"^\+?[0-9]{8,15}$")
            .WithMessage("The phone number must contain 8 to 15 digits.");

        RuleFor(command => command.Amount)
            .GreaterThan(0m)
            .PrecisionScale(18, 2, true);

        RuleFor(command => command.Currency)
            .NotEmpty()
            .Length(3)
            .Matches("^[A-Za-z]{3}$");

        RuleFor(command => command.IdempotencyKey)
            .NotEmpty()
            .MaximumLength(100);
    }
}

internal sealed class TopUpMobileHandler(
    IApplicationDbContext dbContext,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
    : ICommandHandler<TopUpMobileCommand, TopUpMobileResult>
{
    public async Task<Result<TopUpMobileResult>> Handle(
        TopUpMobileCommand request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not Guid userId)
        {
            return Result.Failure<TopUpMobileResult>(
                Error.Unauthorized("Auth.Required", "Sign-in is required."));
        }

        // A retried request returns the original result instead of charging again.
        var existing = await dbContext.MobileTopUps
            .AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.UserId == userId && item.IdempotencyKey == request.IdempotencyKey,
                cancellationToken);

        if (existing is not null)
        {
            var balanceAfter = await dbContext.Transactions
                .AsNoTracking()
                .Where(transaction => transaction.Id == existing.TransactionId)
                .Select(transaction => transaction.BalanceAfter)
                .SingleAsync(cancellationToken);

            return Result.Success(new TopUpMobileResult(
                existing.Id,
                existing.Amount.Amount,
                existing.Amount.Currency,
                balanceAfter,
                existing.CreatedAt));
        }

        var account = await dbContext.Accounts
            .SingleOrDefaultAsync(
                item => item.Id == request.AccountId && item.UserId == userId,
                cancellationToken);

        if (account is null)
        {
            return Result.Failure<TopUpMobileResult>(
                Error.NotFound("Account.NotFound", "The account was not found."));
        }

        if (account.Status != AccountStatus.Active)
        {
            return Result.Failure<TopUpMobileResult>(
                Error.BusinessRule("Account.NotActive", "The account is not active."));
        }

        var amount = Money.Of(request.Amount, request.Currency);

        if (account.Balance.Currency != amount.Currency)
        {
            return Result.Failure<TopUpMobileResult>(
                Error.BusinessRule(
                    "Account.CurrencyMismatch",
                    "The top-up currency must match the account currency."));
        }

        if (!account.HasSufficientFunds(amount))
        {
            return Result.Failure<TopUpMobileResult>(
                Error.BusinessRule(
                    "Account.InsufficientFunds",
                    "The account does not have enough funds."));
        }

        // TODO(OTP): verify the transaction code here once a transaction OTP purpose exists.

        var topUp = MobileTopUp.Execute(
            account,
            userId,
            request.PhoneNumber,
            amount,
            request.IdempotencyKey,
            timeProvider.GetUtcNow());

        dbContext.MobileTopUps.Add(topUp.TopUp);
        dbContext.Transactions.Add(topUp.Debit);

        return Result.Success(new TopUpMobileResult(
            topUp.TopUp.Id,
            topUp.TopUp.Amount.Amount,
            topUp.TopUp.Amount.Currency,
            topUp.Debit.BalanceAfter,
            topUp.TopUp.CreatedAt));
    }
}
