using FluentValidation;
using Limita.Application.Common;
using Limita.Application.Common.Abstractions;
using Limita.Application.Common.Messaging;
using Limita.Application.Features.Auth;
using Limita.Domain.Entities;
using Limita.Domain.Enums;
using Limita.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Limita.Application.Features.Withdrawals.Commands;

public sealed record CreateWithdrawalCommand(
    Guid AccountId,
    decimal Amount,
    string Currency,
    string PhoneNumber) : ICommand<Guid>;

public sealed class CreateWithdrawalValidator
    : AbstractValidator<CreateWithdrawalCommand>
{
    public CreateWithdrawalValidator()
    {
        RuleFor(command => command.AccountId)
            .NotEmpty();

        RuleFor(command => command.Amount)
            .GreaterThan(0);

        RuleFor(command => command.Currency)
            .NotEmpty()
            .Length(3)
            .Matches("^[A-Za-z]{3}$");

        RuleFor(command => command.PhoneNumber)
            .ValidPhoneNumber();
    }
}

internal sealed class CreateWithdrawalHandler(
    IApplicationDbContext dbContext,
    ICurrentUser currentUser,
    ISecretGenerator secretGenerator,
    ISecretHasher secretHasher,
    ISmsSender smsSender,
    TimeProvider timeProvider)
    : ICommandHandler<CreateWithdrawalCommand, Guid>
{
    public async Task<Result<Guid>> Handle(
        CreateWithdrawalCommand request,
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

        // The withdrawal code is sent by SMS, so it may only go to the signed-in user's own verified number.
        // Otherwise any signed-in user could use this endpoint to send SMS to anybody.
        var user = await dbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == userId, cancellationToken);

        if (user is null || !user.IsPhoneVerified
            || AuthInput.NormalizePhone(request.PhoneNumber) != user.PhoneNumber)
        {
            return Result.Failure<Guid>(
                Error.BusinessRule(
                    "Withdrawal.PhoneMismatch",
                    "The withdrawal code can only be sent to your own verified phone number."));
        }

        // Each request sends an SMS, so limit how many unconfirmed withdrawals one user can have open.
        const int maxOpenWithdrawals = 3;
        var openNow = timeProvider.GetUtcNow();

        var openWithdrawals = await dbContext.Withdrawals
            .AsNoTracking()
            .CountAsync(
                item => item.UserId == userId
                    && item.Status == WithdrawalStatus.PendingVerification
                    && item.ExpiresAt > openNow,
                cancellationToken);

        if (openWithdrawals >= maxOpenWithdrawals)
        {
            return Result.Failure<Guid>(
                Error.TooManyRequests(
                    "Withdrawal.TooManyOpen",
                    "Please confirm or wait for your open withdrawals to expire before creating another."));
        }

        var amount = Money.Of(request.Amount, request.Currency);

        if (account.Balance.Currency != amount.Currency)
        {
            return Result.Failure<Guid>(
                Error.BusinessRule(
                    "Account.CurrencyMismatch",
                    "The withdrawal currency must match the account currency."));
        }

        if (!account.HasSufficientFunds(amount))
        {
            return Result.Failure<Guid>(
                Error.BusinessRule(
                    "Account.InsufficientFunds",
                    "The account does not have enough funds."));
        }

        var code = secretGenerator.NewOtpCode();
        var codeHash = secretHasher.Hash(code);
        var now = timeProvider.GetUtcNow();

        var withdrawal = Withdrawal.Create(
            userId,
            account.Id,
            request.PhoneNumber,
            amount,
            codeHash,
            now);

        dbContext.Withdrawals.Add(withdrawal);

        await smsSender.SendAsync(
            request.PhoneNumber,
            $"Your Limita withdrawal code is {code}. It expires in 5 minutes.",
            cancellationToken);

        return Result.Success(withdrawal.Id);
    }
}