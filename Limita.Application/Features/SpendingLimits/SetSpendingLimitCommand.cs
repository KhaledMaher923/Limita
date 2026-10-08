using FluentValidation;
using Limita.Application.Common;
using Limita.Application.Common.Abstractions;
using Limita.Application.Common.Messaging;
using Limita.Domain.Entities;
using Limita.Domain.Enums;
using Limita.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Limita.Application.Features.SpendingLimits;

public sealed record SetSpendingLimitCommand(
    Guid AccountId,
    decimal Amount,
    string Currency,
    TransactionCategory? Category) : ICommand<Guid>;

public sealed class SetSpendingLimitValidator
    : AbstractValidator<SetSpendingLimitCommand>
{
    public SetSpendingLimitValidator()
    {
        RuleFor(command => command.AccountId).NotEmpty();
        RuleFor(command => command.Amount).GreaterThan(0);
        RuleFor(command => command.Currency)
            .Length(3)
            .Matches("^[A-Za-z]{3}$");
    }
}

internal sealed class SetSpendingLimitHandler(
    IApplicationDbContext dbContext,
    ICurrentUser currentUser)
    : ICommandHandler<SetSpendingLimitCommand, Guid>
{
    public async Task<Result<Guid>> Handle(
        SetSpendingLimitCommand request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not Guid userId)
        {
            return Result.Failure<Guid>(
                Error.Unauthorized("Auth.Required", "Sign-in is required."));
        }

        var account = await dbContext.Accounts
            .SingleOrDefaultAsync(
                item => item.Id == request.AccountId
                    && item.UserId == userId,
                cancellationToken);

        if (account is null)
        {
            return Result.Failure<Guid>(
                Error.NotFound("Account.NotFound", "Account was not found."));
        }

        if (account.Balance.Currency != request.Currency.ToUpperInvariant())
        {
            return Result.Failure<Guid>(
                Error.Validation(
                    "SpendingLimit.CurrencyMismatch",
                    "The limit currency must match the account currency."));
        }

        var limit = SpendingLimit.Create(
            userId,
            account.Id,
            null,
            request.Category,
            LimitPeriod.Monthly,
            Money.Of(request.Amount, request.Currency));

        dbContext.SpendingLimits.Add(limit);

        return Result.Success(limit.Id);
    }
}