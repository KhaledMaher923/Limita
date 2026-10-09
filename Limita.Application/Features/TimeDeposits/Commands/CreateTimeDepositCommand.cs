using FluentValidation;
using Limita.Application.Common;
using Limita.Application.Common.Abstractions;
using Limita.Application.Common.Messaging;
using Limita.Domain.Entities;
using Limita.Domain.Enums;
using Limita.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Limita.Application.Features.TimeDeposits.Commands;

public sealed record CreateTimeDepositCommand(
    Guid AccountId,
    decimal Amount,
    string Currency,
    int TermInMonths) : ICommand<Guid>;

public sealed class CreateTimeDepositValidator
    : AbstractValidator<CreateTimeDepositCommand>
{
    public CreateTimeDepositValidator()
    {
        RuleFor(command => command.AccountId)
            .NotEmpty();

        RuleFor(command => command.Amount)
            .GreaterThanOrEqualTo(DepositTermCatalog.MinimumPrincipal);

        RuleFor(command => command.Currency)
            .NotEmpty()
            .Length(3)
            .Matches("^[A-Za-z]{3}$");

        RuleFor(command => command.TermInMonths)
            .Must(term => DepositTermCatalog.Find(term) is not null)
            .WithMessage("The selected deposit term is not supported.");
    }
}

internal sealed class CreateTimeDepositHandler(
    IApplicationDbContext dbContext,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
    : ICommandHandler<CreateTimeDepositCommand, Guid>
{
    public async Task<Result<Guid>> Handle(
        CreateTimeDepositCommand request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not Guid userId)
        {
            return Result.Failure<Guid>(
                Error.Unauthorized("Auth.Required", "Sign-in is required."));
        }

        var account = await dbContext.Accounts
            .SingleOrDefaultAsync(
                item => item.Id == request.AccountId && item.UserId == userId,
                cancellationToken);

        if (account is null)
        {
            return Result.Failure<Guid>(
                Error.NotFound("Account.NotFound", "The account was not found."));
        }

        var term = DepositTermCatalog.Find(request.TermInMonths);
        if (term is null)
        {
            return Result.Failure<Guid>(
                Error.Validation("Deposit.Term.Invalid", "The selected term is not supported."));
        }

        var principal = Money.Of(request.Amount, request.Currency);

        if (account.Balance.Currency != principal.Currency)
        {
            return Result.Failure<Guid>(
                Error.BusinessRule(
                    "Account.CurrencyMismatch",
                    "The deposit currency must match the account currency."));
        }

        if (!account.HasSufficientFunds(principal))
        {
            return Result.Failure<Guid>(
                Error.BusinessRule(
                    "Account.InsufficientFunds",
                    "The account does not have enough funds."));
        }

        var now = timeProvider.GetUtcNow();

        var deposit = TimeDeposit.Create(
            userId,
            account.Id,
            principal,
            term.TermInMonths,
            term.AnnualInterestRatePercent,
            now);

        var ledgerEntry = account.Debit(
            principal,
            TransactionCategory.Savings,
            $"Time deposit ({term.TermInMonths} months)",
            now,
            timeDepositId: deposit.Id);

        dbContext.TimeDeposits.Add(deposit);
        dbContext.Transactions.Add(ledgerEntry);

        return Result.Success(deposit.Id);
    }
}