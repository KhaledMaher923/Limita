using FluentValidation;
using Limita.Application.Common;
using Limita.Application.Common.Abstractions;
using Limita.Application.Common.Messaging;
using Limita.Domain.Entities;
using Limita.Domain.Enums;
using Limita.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Limita.Application.Features.Transfers.Commands;

/// <summary>
/// Sends money to a beneficiary outside Limita. The amount and the fee leave the account immediately
/// and the transfer stays Pending until the receiving bank confirms it.
/// Send an IdempotencyKey (any unique string per attempt, for example a GUID) so a retried request never charges twice.
/// </summary>
public sealed record CreateTransferCommand(
    Guid FromAccountId,
    TransferType TransferType,
    VerificationMethod VerificationMethod,
    decimal Amount,
    string Currency,
    Guid? BeneficiaryId,
    string? Note,
    string? IdempotencyKey = null) : ICommand<CreateTransferResult>;

public sealed record CreateTransferResult(
    Guid TransferId,
    string Status,
    string TransferType,
    string VerificationMethod,
    decimal Amount,
    decimal Fee,
    string Currency,
    DateTimeOffset CreatedAt);

public sealed class CreateTransferValidator : AbstractValidator<CreateTransferCommand>
{
    public CreateTransferValidator()
    {
        RuleFor(x => x.FromAccountId).NotEmpty();
        RuleFor(x => x.TransferType).IsInEnum();
        RuleFor(x => x.VerificationMethod).IsInEnum();
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Currency).NotEmpty().Length(3);
        RuleFor(x => x.BeneficiaryId).NotNull().WithMessage("A beneficiary is required.");
        RuleFor(x => x.Note).MaximumLength(200);
        RuleFor(x => x.IdempotencyKey).MaximumLength(100);
    }
}

internal sealed class CreateTransferHandler(
    IApplicationDbContext dbContext,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
    : ICommandHandler<CreateTransferCommand, CreateTransferResult>
{
    public async Task<Result<CreateTransferResult>> Handle(
        CreateTransferCommand request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not Guid userId)
            return TransferErrors.AuthRequired;

        // The account is tracked (no AsNoTracking) because its balance changes below.
        var account = await dbContext.Accounts
            .SingleOrDefaultAsync(
                a => a.Id == request.FromAccountId && a.UserId == userId,
                cancellationToken);

        if (account is null)
            return TransferErrors.AccountNotFound;

        // A retried request returns the original transfer instead of charging again.
        // Without a key the request is treated as new, so it has no retry protection.
        var idempotencyKey = string.IsNullOrWhiteSpace(request.IdempotencyKey)
            ? Guid.NewGuid().ToString("N")
            : request.IdempotencyKey.Trim();

        var existing = await dbContext.Transfers
            .AsNoTracking()
            .SingleOrDefaultAsync(
                t => t.FromAccountId == account.Id && t.IdempotencyKey == idempotencyKey,
                cancellationToken);
       
        if (existing is not null)
            return ToResult(existing);

        var beneficiary = await dbContext.Beneficiaries
            .AsNoTracking()
            .SingleOrDefaultAsync(
                b => b.Id == request.BeneficiaryId!.Value && b.UserId == userId,
                cancellationToken);

        if (beneficiary is null)
            return TransferErrors.BeneficiaryNotFound;
       
        if (beneficiary.TransferType != request.TransferType)
            return TransferErrors.BeneficiaryTypeMismatch;

        if (account.Status != AccountStatus.Active)
            return TransferErrors.AccountNotActive;

        var currency = request.Currency.ToUpperInvariant();

        if (currency != account.Balance.Currency)
            return TransferErrors.CurrencyMismatch;

        var amount = Money.Of(request.Amount, currency);
        var fee = Money.Of(TransferSettings.TransferFeeAmount, currency);

        if (!account.HasSufficientFunds(amount + fee))
            return TransferErrors.InsufficientFunds;

        // TODO(OTP / biometrics): VerificationMethod is stored but not enforced yet.

        var created = Transfer.CreateExternal(
            account,
            userId,
            request.TransferType,
            request.VerificationMethod,
            amount,
            fee,
            beneficiary.Id,
            idempotencyKey,
            request.Note,
            timeProvider.GetUtcNow());

        dbContext.Transfers.Add(created.Transfer);
        dbContext.Transactions.Add(created.TransferDebit);

        if (created.FeeDebit is not null)
            dbContext.Transactions.Add(created.FeeDebit);

        return ToResult(created.Transfer);
    }

    private static CreateTransferResult ToResult(Transfer transfer) => new(
        transfer.Id,
        transfer.Status.ToString(),
        transfer.TransferType.ToString(),
        transfer.VerificationMethod.ToString(),
        transfer.Amount.Amount,
        transfer.Fee.Amount,
        transfer.Amount.Currency,
        transfer.CreatedAt);

}
