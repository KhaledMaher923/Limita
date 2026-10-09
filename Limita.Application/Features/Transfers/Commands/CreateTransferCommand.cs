using FluentValidation;
using Limita.Application.Common;
using Limita.Application.Common.Abstractions;
using Limita.Application.Common.Messaging;
using Limita.Domain.Entities;
using Limita.Domain.Enums;
using Limita.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Limita.Application.Features.Transfers.Commands;

public sealed record CreateTransferCommand(
    Guid FromAccountId,
    TransferType TransferType,
    VerificationMethod VerificationMethod,
    decimal Amount,
    string Currency,
    Guid? BeneficiaryId,
    string? Note) : ICommand<CreateTransferResult>;

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
        RuleFor(x => x.Note).MaximumLength(200);
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

        // Validate account belongs to user
        var account = await dbContext.Accounts
            .AsNoTracking()
            .SingleOrDefaultAsync(
                a => a.Id == request.FromAccountId && a.UserId == userId,
                cancellationToken);

        if (account is null)
            return TransferErrors.AccountNotFound;

        // Validate beneficiary if provided
        if (request.BeneficiaryId.HasValue)
        {
            var beneficiary = await dbContext.Beneficiaries
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    b => b.Id == request.BeneficiaryId.Value && b.UserId == userId,
                    cancellationToken);

            if (beneficiary is null)
                return TransferErrors.BeneficiaryNotFound;
        }

        var amount = Money.Of(request.Amount, request.Currency.ToUpperInvariant());
        var fee = Money.Of(TransferSettings.TransferFeeAmount, request.Currency.ToUpperInvariant());

        var now = timeProvider.GetUtcNow();

        // Transfer stays Pending because ledger / balance deduction is another member's responsibility.
        var transfer = Transfer.CreatePending(
            userId,
            request.FromAccountId,
            request.TransferType,
            request.VerificationMethod,
            amount,
            fee,
            request.BeneficiaryId,
            request.Note,
            now);

        dbContext.Transfers.Add(transfer);

        return Result.Success(new CreateTransferResult(
            transfer.Id,
            transfer.Status.ToString(),
            transfer.TransferType.ToString(),
            transfer.VerificationMethod.ToString(),
            transfer.Amount.Amount,
            fee.Amount,
            transfer.Amount.Currency,
            transfer.CreatedAt));
    }
}
