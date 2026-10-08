using FluentValidation;
using Limita.Application.Common;
using Limita.Application.Common.Abstractions;
using Limita.Application.Common.Messaging;
using Limita.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Limita.Application.Features.Withdrawals.Commands;

public sealed record ConfirmWithdrawalCommand(
    Guid WithdrawalId,
    string Code) : ICommand<ConfirmWithdrawalOutcome>;

public sealed record ConfirmWithdrawalOutcome(
    bool IsConfirmed,
    string? ErrorMessage,
    int AttemptsRemaining);

public sealed class ConfirmWithdrawalValidator
    : AbstractValidator<ConfirmWithdrawalCommand>
{
    public ConfirmWithdrawalValidator()
    {
        RuleFor(command => command.WithdrawalId)
            .NotEmpty();

        RuleFor(command => command.Code)
            .NotEmpty()
            .Length(6)
            .Matches("^[0-9]{6}$");
    }
}

internal sealed class ConfirmWithdrawalHandler(
    IApplicationDbContext dbContext,
    ICurrentUser currentUser,
    ISecretHasher secretHasher,
    TimeProvider timeProvider)
    : ICommandHandler<ConfirmWithdrawalCommand, ConfirmWithdrawalOutcome>
{
    public async Task<Result<ConfirmWithdrawalOutcome>> Handle(
        ConfirmWithdrawalCommand request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not Guid userId)
        {
            return Result.Failure<ConfirmWithdrawalOutcome>(
                Error.Unauthorized("Auth.Required", "Sign-in is required."));
        }

        var withdrawal = await dbContext.Withdrawals
            .SingleOrDefaultAsync(
                item => item.Id == request.WithdrawalId
                    && item.UserId == userId,
                cancellationToken);

        if (withdrawal is null)
        {
            return Result.Failure<ConfirmWithdrawalOutcome>(
                Error.NotFound(
                    "Withdrawal.NotFound",
                    "The withdrawal was not found."));
        }

        if (withdrawal.Status != WithdrawalStatus.PendingVerification)
        {
            return Result.Failure<ConfirmWithdrawalOutcome>(
                Error.Conflict(
                    "Withdrawal.NotPending",
                    "The withdrawal is not awaiting verification."));
        }

        var now = timeProvider.GetUtcNow();

        if (withdrawal.IsExpired(now))
        {
            withdrawal.MarkExpired(now);

            return Result.Success(
                new ConfirmWithdrawalOutcome(
                    false,
                    "The withdrawal code has expired.",
                    0));
        }

        if (!secretHasher.Verify(request.Code, withdrawal.CodeHash))
        {
            var remainingAttempts =
                withdrawal.RegisterFailedVerificationAttempt();

            var errorMessage = remainingAttempts == 0
                ? "The maximum number of code attempts has been reached."
                : "The withdrawal code is incorrect.";

            return Result.Success(
                new ConfirmWithdrawalOutcome(
                    false,
                    errorMessage,
                    remainingAttempts));
        }

        var account = await dbContext.Accounts
            .SingleOrDefaultAsync(
                item => item.Id == withdrawal.AccountId
                    && item.UserId == userId,
                cancellationToken);

        if (account is null)
        {
            return Result.Failure<ConfirmWithdrawalOutcome>(
                Error.NotFound(
                    "Account.NotFound",
                    "The account was not found."));
        }

        withdrawal.Complete(now);

        var ledgerEntry = account.Debit(
            withdrawal.Amount,
            TransactionCategory.Other,
            "Cash withdrawal",
            now,
            withdrawalId: withdrawal.Id);

        dbContext.Transactions.Add(ledgerEntry);

        return Result.Success(
            new ConfirmWithdrawalOutcome(true, null, 0));
    }
}