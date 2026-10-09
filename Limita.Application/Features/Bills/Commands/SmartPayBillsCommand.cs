using FluentValidation;
using Limita.Application.Common;
using Limita.Application.Common.Abstractions;
using Limita.Application.Common.Messaging;
using Limita.Domain.Entities;
using Limita.Domain.Enums;
using Limita.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Limita.Application.Features.Bills.Commands;

/// <summary>Pays several of the customer's own unpaid bills in one request: all of them or none.</summary>
public sealed record SmartPayBillsCommand(
    Guid AccountId,
    IReadOnlyList<Guid> BillIds,
    string IdempotencyKey) : ICommand<SmartPayResult>;

public sealed record SmartPayResult(
    IReadOnlyList<Guid> PaymentIds,
    decimal TotalPaid,
    string Currency,
    decimal BalanceAfter);

public sealed class SmartPayBillsValidator : AbstractValidator<SmartPayBillsCommand>
{
    public SmartPayBillsValidator()
    {
        RuleFor(command => command.AccountId).NotEmpty();

        RuleFor(command => command.BillIds)
            .NotEmpty()
            .Must(ids => ids.Count <= 20)
            .WithMessage("You can pay at most 20 bills at once.")
            .Must(ids => ids.Distinct().Count() == ids.Count)
            .WithMessage("The same bill appears more than once.");

        // Each payment key is "<key>:<billId>" (37 extra characters) and must fit in 100.
        RuleFor(command => command.IdempotencyKey)
            .NotEmpty()
            .MaximumLength(60);
    }
}

internal sealed class SmartPayBillsHandler(
    IApplicationDbContext dbContext,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
    : ICommandHandler<SmartPayBillsCommand, SmartPayResult>
{
    public async Task<Result<SmartPayResult>> Handle(
        SmartPayBillsCommand request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not Guid userId)
        {
            return Result.Failure<SmartPayResult>(
                Error.Unauthorized("Auth.Required", "Sign-in is required."));
        }

        // A retried request returns the original result instead of charging again.
        var prefix = request.IdempotencyKey + ":";

        var done = await dbContext.BillPayments
            .AsNoTracking()
            .Where(item => item.UserId == userId && item.IdempotencyKey.StartsWith(prefix))
            .ToListAsync(cancellationToken);

        if (done.Count > 0)
        {
            var transactionIds = done.Select(item => item.TransactionId).ToList();

            // Balances only go down inside one request, so the smallest one belongs to the last debit.
            var balanceAfter = await dbContext.Transactions
                .AsNoTracking()
                .Where(transaction => transactionIds.Contains(transaction.Id))
                .MinAsync(transaction => transaction.BalanceAfter, cancellationToken);

            return Result.Success(new SmartPayResult(
                done.Select(item => item.Id).ToList(),
                done.Sum(item => item.Amount.Amount),
                done[0].Amount.Currency,
                balanceAfter));
        }

        var account = await dbContext.Accounts
            .SingleOrDefaultAsync(
                item => item.Id == request.AccountId && item.UserId == userId,
                cancellationToken);

        if (account is null)
        {
            return Result.Failure<SmartPayResult>(
                Error.NotFound("Account.NotFound", "The account was not found."));
        }

        if (account.Status != AccountStatus.Active)
        {
            return Result.Failure<SmartPayResult>(
                Error.BusinessRule("Account.NotActive", "The account is not active."));
        }

        var billIds = request.BillIds.ToList();

        var bills = await dbContext.Bills
            .Where(bill => billIds.Contains(bill.Id)
                && bill.CustomerUserId == userId
                && bill.Status == BillStatus.Unpaid)
            .OrderBy(bill => bill.DueDate)
            .ToListAsync(cancellationToken);

        if (bills.Count != billIds.Count)
        {
            return Result.Failure<SmartPayResult>(
                Error.NotFound(
                    "SmartPay.BillsNotAvailable",
                    "One or more bills were not found, are already paid, or are not yours."));
        }

        if (bills.Any(bill => bill.Total.Currency != account.Balance.Currency))
        {
            return Result.Failure<SmartPayResult>(
                Error.BusinessRule(
                    "Account.CurrencyMismatch",
                    "The bill currency must match the account currency."));
        }

        var total = bills.Aggregate(
            Money.Zero(account.Balance.Currency),
            (sum, bill) => sum + bill.Total);

        if (!account.HasSufficientFunds(total))
        {
            return Result.Failure<SmartPayResult>(
                Error.BusinessRule(
                    "Account.InsufficientFunds",
                    "The account does not have enough funds."));
        }

        // TODO(OTP): one verification for the whole request, once a transaction OTP purpose exists.

        var now = timeProvider.GetUtcNow();
        var payments = new List<BillPaymentResult>();

        foreach (var bill in bills)
        {
            var payment = BillPayment.Execute(
                account,
                bill,
                userId,
                $"{request.IdempotencyKey}:{bill.Id}",
                now);

            dbContext.BillPayments.Add(payment.Payment);
            dbContext.Transactions.Add(payment.Debit);
            payments.Add(payment);
        }

        return Result.Success(new SmartPayResult(
            payments.Select(item => item.Payment.Id).ToList(),
            total.Amount,
            total.Currency,
            payments[^1].Debit.BalanceAfter));
    }
}
