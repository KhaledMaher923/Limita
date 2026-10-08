using FluentValidation;
using Limita.Application.Common;
using Limita.Application.Common.Abstractions;
using Limita.Application.Common.Messaging;
using Limita.Domain.Entities;
using Limita.Domain.Enums;
using Limita.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Limita.Application.Features.Exchange.Commands;

public sealed record ExchangeCurrencyCommand(
    Guid SourceAccountId,
    Guid TargetAccountId,
    decimal Amount) : ICommand<Guid>;

public sealed class ExchangeCurrencyValidator
    : AbstractValidator<ExchangeCurrencyCommand>
{
    public ExchangeCurrencyValidator()
    {
        RuleFor(command => command.SourceAccountId).NotEmpty();
        RuleFor(command => command.TargetAccountId).NotEmpty();
        RuleFor(command => command.Amount).GreaterThan(0);

        RuleFor(command => command)
            .Must(command =>
                command.SourceAccountId != command.TargetAccountId)
            .WithMessage("Choose two different accounts.");
    }
}

internal sealed class ExchangeCurrencyHandler(
    IApplicationDbContext dbContext,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
    : ICommandHandler<ExchangeCurrencyCommand, Guid>
{
    public async Task<Result<Guid>> Handle(
        ExchangeCurrencyCommand request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not Guid userId)
        {
            return Result.Failure<Guid>(
                Error.Unauthorized("Auth.Required", "Sign-in is required."));
        }

        var accounts = await dbContext.Accounts
            .Where(account =>
                account.UserId == userId
                && (account.Id == request.SourceAccountId
                    || account.Id == request.TargetAccountId))
            .ToListAsync(cancellationToken);

        var sourceAccount = accounts
            .SingleOrDefault(account => account.Id == request.SourceAccountId);

        var targetAccount = accounts
            .SingleOrDefault(account => account.Id == request.TargetAccountId);

        if (sourceAccount is null || targetAccount is null)
        {
            return Result.Failure<Guid>(
                Error.NotFound(
                    "Exchange.AccountNotFound",
                    "Both accounts must belong to the signed-in user."));
        }

        var sourceCurrency =
            ExchangeCurrencyCatalog.Find(sourceAccount.Balance.Currency);

        var targetCurrency =
            ExchangeCurrencyCatalog.Find(targetAccount.Balance.Currency);

        if (sourceCurrency is null || targetCurrency is null)
        {
            return Result.Failure<Guid>(
                Error.Validation(
                    "Exchange.Currency.Unsupported",
                    "One of the account currencies is not supported."));
        }

        if (sourceCurrency.Code == targetCurrency.Code)
        {
            return Result.Failure<Guid>(
                Error.Validation(
                    "Exchange.Currency.Same",
                    "The account currencies must differ."));
        }

        var sourceAmount = Money.Of(
            request.Amount,
            sourceCurrency.Code);

        if (!sourceAccount.HasSufficientFunds(sourceAmount))
        {
            return Result.Failure<Guid>(
                Error.BusinessRule(
                    "Account.InsufficientFunds",
                    "The source account does not have enough funds."));
        }

        var convertedAmount = ExchangeCurrencyCatalog.Convert(
            request.Amount,
            sourceCurrency,
            targetCurrency);

        var targetAmount = Money.Of(
            convertedAmount,
            targetCurrency.Code);

        var exchangeRate = ExchangeCurrencyCatalog.Convert(
            1m,
            sourceCurrency,
            targetCurrency);

        var now = timeProvider.GetUtcNow();

        var operation = ExchangeOperation.Create(
            userId,
            sourceAccount.Id,
            targetAccount.Id,
            sourceAmount,
            targetAmount,
            exchangeRate,
            now);

        var debit = sourceAccount.Debit(
            sourceAmount,
            TransactionCategory.Exchange,
            $"Exchange to {targetCurrency.Code}",
            now,
            exchangeOperationId: operation.Id);

        var credit = targetAccount.Credit(
            targetAmount,
            TransactionCategory.Exchange,
            $"Exchange from {sourceCurrency.Code}",
            now,
            exchangeOperationId: operation.Id);

        dbContext.ExchangeOperations.Add(operation);
        dbContext.Transactions.Add(debit);
        dbContext.Transactions.Add(credit);

        return Result.Success(operation.Id);
    }
}