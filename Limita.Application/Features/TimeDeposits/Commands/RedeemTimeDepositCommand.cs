using FluentValidation;
using Limita.Application.Common;
using Limita.Application.Common.Abstractions;
using Limita.Application.Common.Messaging;
using Limita.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Limita.Application.Features.TimeDeposits.Commands;

public sealed record RedeemTimeDepositCommand(
    Guid TimeDepositId) : ICommand<RedeemTimeDepositResponse>;

public sealed record RedeemTimeDepositResponse(
    Guid TimeDepositId,
    decimal Amount,
    string Currency);

public sealed class RedeemTimeDepositValidator
    : AbstractValidator<RedeemTimeDepositCommand>
{
    public RedeemTimeDepositValidator()
    {
        RuleFor(command => command.TimeDepositId)
            .NotEmpty();
    }
}

internal sealed class RedeemTimeDepositHandler(
    IApplicationDbContext dbContext,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
    : ICommandHandler<RedeemTimeDepositCommand, RedeemTimeDepositResponse>
{
    public async Task<Result<RedeemTimeDepositResponse>> Handle(
        RedeemTimeDepositCommand request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not Guid userId)
        {
            return Result.Failure<RedeemTimeDepositResponse>(
                Error.Unauthorized("Auth.Required", "Sign-in is required."));
        }

        var deposit = await dbContext.TimeDeposits
            .SingleOrDefaultAsync(
                item => item.Id == request.TimeDepositId
                    && item.UserId == userId,
                cancellationToken);

        if (deposit is null)
        {
            return Result.Failure<RedeemTimeDepositResponse>(
                Error.NotFound(
                    "TimeDeposit.NotFound",
                    "The time deposit was not found."));
        }

        if (deposit.Status == TimeDepositStatus.Redeemed)
        {
            return Result.Failure<RedeemTimeDepositResponse>(
                Error.Conflict(
                    "TimeDeposit.AlreadyRedeemed",
                    "The time deposit has already been redeemed."));
        }

        var now = timeProvider.GetUtcNow();

        if (now < deposit.MaturesAt)
        {
            return Result.Failure<RedeemTimeDepositResponse>(
                Error.BusinessRule(
                    "TimeDeposit.NotMatured",
                    "The time deposit cannot be redeemed before maturity."));
        }

        var account = await dbContext.Accounts
            .SingleOrDefaultAsync(
                item => item.Id == deposit.FundingAccountId
                    && item.UserId == userId,
                cancellationToken);

        if (account is null)
        {
            return Result.Failure<RedeemTimeDepositResponse>(
                Error.NotFound(
                    "Account.NotFound",
                    "The funding account was not found."));
        }

        var amountToPay = deposit.Redeem(now);

        var ledgerEntry = account.Credit(
            amountToPay,
            TransactionCategory.Savings,
            $"Matured time deposit ({deposit.TermInMonths} months)",
            now,
            timeDepositId: deposit.Id);

        dbContext.Transactions.Add(ledgerEntry);

        return Result.Success(
            new RedeemTimeDepositResponse(
                deposit.Id,
                amountToPay.Amount,
                amountToPay.Currency));
    }
}
