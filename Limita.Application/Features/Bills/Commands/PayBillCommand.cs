using FluentValidation;
using Limita.Application.Common;
using Limita.Application.Common.Abstractions;
using Limita.Application.Common.Messaging;
using Limita.Domain.Entities;
using Limita.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Limita.Application.Features.Bills.Commands;

/// <summary>Pays one bill from one of the customer's accounts. A retried request with the same IdempotencyKey never pays twice.</summary>
public sealed record PayBillCommand(
    BillType Type,
    string Code,
    Guid AccountId,
    string IdempotencyKey) : ICommand<PayBillResult>;

public sealed record PayBillResult(
    Guid PaymentId,
    Guid BillId,
    string Type,
    decimal Amount,
    string Currency,
    decimal BalanceAfter,
    DateTimeOffset PaidAt);

public sealed class PayBillValidator : AbstractValidator<PayBillCommand>
{
    public PayBillValidator()
    {
        RuleFor(command => command.Type).IsInEnum();

        RuleFor(command => command.Code)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(command => command.AccountId).NotEmpty();

        RuleFor(command => command.IdempotencyKey)
            .NotEmpty()
            .MaximumLength(100);
    }
}

internal sealed class PayBillHandler(
    IApplicationDbContext dbContext,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
    : ICommandHandler<PayBillCommand, PayBillResult>
{
    public async Task<Result<PayBillResult>> Handle(
        PayBillCommand request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not Guid userId)
        {
            return Result.Failure<PayBillResult>(
                Error.Unauthorized("Auth.Required", "Sign-in is required."));
        }

        // A retried request returns the original result instead of charging again.
        var existing = await dbContext.BillPayments
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

            return Result.Success(new PayBillResult(
                existing.Id,
                existing.BillId,
                existing.BillType.ToString(),
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
            return Result.Failure<PayBillResult>(
                Error.NotFound("Account.NotFound", "The account was not found."));
        }

        var code = request.Code.Trim();

        var bill = await dbContext.Bills
            .SingleOrDefaultAsync(
                item => item.Type == request.Type && item.Code == code,
                cancellationToken);

        if (bill is null)
        {
            return Result.Failure<PayBillResult>(
                Error.NotFound("Bill.NotFound", "No bill was found for this code."));
        }

        if (bill.IsPaid)
        {
            return Result.Failure<PayBillResult>(
                Error.Conflict("Bill.AlreadyPaid", "This bill is already paid."));
        }

        if (account.Status != AccountStatus.Active)
        {
            return Result.Failure<PayBillResult>(
                Error.BusinessRule("Account.NotActive", "The account is not active."));
        }

        var amount = bill.Total;

        if (account.Balance.Currency != amount.Currency)
        {
            return Result.Failure<PayBillResult>(
                Error.BusinessRule(
                    "Account.CurrencyMismatch",
                    "The bill currency must match the account currency."));
        }

        if (!account.HasSufficientFunds(amount))
        {
            return Result.Failure<PayBillResult>(
                Error.BusinessRule(
                    "Account.InsufficientFunds",
                    "The account does not have enough funds."));
        }

        // TODO(OTP): verify the transaction code here once a transaction OTP purpose exists.

        var payment = BillPayment.Execute(
            account,
            bill,
            userId,
            request.IdempotencyKey,
            timeProvider.GetUtcNow());

        dbContext.BillPayments.Add(payment.Payment);
        dbContext.Transactions.Add(payment.Debit);

        return Result.Success(new PayBillResult(
            payment.Payment.Id,
            bill.Id,
            bill.Type.ToString(),
            payment.Payment.Amount.Amount,
            payment.Payment.Amount.Currency,
            payment.Debit.BalanceAfter,
            payment.Payment.CreatedAt));
    }
}
