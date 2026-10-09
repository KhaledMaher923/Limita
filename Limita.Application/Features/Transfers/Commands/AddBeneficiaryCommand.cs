using FluentValidation;
using Limita.Application.Common;
using Limita.Application.Common.Abstractions;
using Limita.Application.Common.Messaging;
using Limita.Domain.Entities;
using Limita.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Limita.Application.Features.Transfers.Commands;

public sealed record AddBeneficiaryCommand(
    string Name,
    TransferType TransferType,
    string? CardNumber,
    string? AccountNumber,
    Guid? BankId,
    Guid? BranchId) : ICommand<AddBeneficiaryResult>;

public sealed record AddBeneficiaryResult(
    Guid BeneficiaryId,
    string Name,
    string TransferType,
    string? MaskedCardNumber,
    string? AccountNumber,
    Guid? BankId,
    Guid? BranchId);

public sealed class AddBeneficiaryValidator : AbstractValidator<AddBeneficiaryCommand>
{
    public AddBeneficiaryValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.TransferType).IsInEnum();

        When(x => x.TransferType == TransferType.CardNumber, () =>
        {
            RuleFor(x => x.CardNumber)
                .NotEmpty()
                .Length(13, 19)
                .Must(v => v != null && v.All(char.IsAsciiDigit))
                .WithMessage("Card number must contain only digits.");
        });

        When(x => x.TransferType != TransferType.CardNumber, () =>
        {
            RuleFor(x => x.AccountNumber).NotEmpty().MaximumLength(30);
            RuleFor(x => x.BankId).NotEmpty();
            RuleFor(x => x.BranchId).NotEmpty();
        });
    }
}

internal sealed class AddBeneficiaryHandler(
    IApplicationDbContext dbContext,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
    : ICommandHandler<AddBeneficiaryCommand, AddBeneficiaryResult>
{
    public async Task<Result<AddBeneficiaryResult>> Handle(
        AddBeneficiaryCommand request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not Guid userId)
            return TransferErrors.AuthRequired;

        Beneficiary beneficiary;

        if (request.TransferType == TransferType.CardNumber)
        {
            var last4 = request.CardNumber![^4..];
            // In production, use a proper tokenisation service. Here we SHA-256 hash it.
            var hash = Convert.ToHexString(
                System.Security.Cryptography.SHA256.HashData(
                    System.Text.Encoding.UTF8.GetBytes(request.CardNumber!)));

            beneficiary = Beneficiary.CreateForCard(
                userId,
                request.Name,
                last4,
                hash,
                timeProvider.GetUtcNow());
        }
        else
        {
            var bankExists = await dbContext.Banks
                .AnyAsync(b => b.Id == request.BankId!.Value, cancellationToken);

            if (!bankExists)
                return TransferErrors.BankNotFound;

            var branch = await dbContext.BankBranches
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    b => b.Id == request.BranchId!.Value,
                    cancellationToken);

            if (branch is null)
                return TransferErrors.BranchNotFound;

            if (branch.BankId != request.BankId!.Value)
                return TransferErrors.BranchNotInBank;

            beneficiary = Beneficiary.CreateForBank(
                userId,
                request.Name,
                request.TransferType,
                request.AccountNumber!,
                request.BankId!.Value,
                request.BranchId!.Value,
                timeProvider.GetUtcNow());
        }

        dbContext.Beneficiaries.Add(beneficiary);

        return Result.Success(new AddBeneficiaryResult(
            beneficiary.Id,
            beneficiary.Name,
            beneficiary.TransferType.ToString(),
            beneficiary.MaskedCardNumber,
            beneficiary.AccountNumber,
            beneficiary.BankId,
            beneficiary.BranchId));
    }
}
