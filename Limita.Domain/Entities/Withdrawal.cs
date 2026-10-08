using Limita.Domain.Common;
using Limita.Domain.Enums;
using Limita.Domain.ValueObjects;

namespace Limita.Domain.Entities;

public sealed class Withdrawal : Entity
{
    public const int MaximumVerificationAttempts = 3;

    private Withdrawal()
    {
        // EF Core needs a private constructor.
    }

    public Guid UserId { get; private set; }
    public Guid AccountId { get; private set; }
    public string PhoneNumber { get; private set; } = string.Empty;
    public Money Amount { get; private set; } = null!;
    public string CodeHash { get; private set; } = string.Empty;
    public WithdrawalStatus Status { get; private set; }
    public int FailedVerificationAttempts { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }

    public static Withdrawal Create(
        Guid userId,
        Guid accountId,
        string phoneNumber,
        Money amount,
        string codeHash,
        DateTimeOffset createdAt)
    {
        if (userId == Guid.Empty)
            throw new DomainException("User is required.");

        if (accountId == Guid.Empty)
            throw new DomainException("Account is required.");

        Guard.NotBlank(phoneNumber, "Phone number", 20);
        Guard.Positive(amount, "Withdrawal amount");
        Guard.NotBlank(codeHash, "Code hash", 128);

        var utcCreatedAt = createdAt.ToUniversalTime();

        return new Withdrawal
        {
            UserId = userId,
            AccountId = accountId,
            PhoneNumber = phoneNumber.Trim(),
            Amount = amount,
            CodeHash = codeHash,
            Status = WithdrawalStatus.PendingVerification,
            FailedVerificationAttempts = 0,
            CreatedAt = utcCreatedAt,
            ExpiresAt = utcCreatedAt.AddMinutes(5)
        };
    }

    public bool IsExpired(DateTimeOffset now)
        => now.ToUniversalTime() >= ExpiresAt;

    public int RegisterFailedVerificationAttempt()
    {
        if (Status != WithdrawalStatus.PendingVerification)
            throw new DomainException("The withdrawal is not awaiting verification.");

        FailedVerificationAttempts++;

        if (FailedVerificationAttempts >= MaximumVerificationAttempts)
            Status = WithdrawalStatus.Cancelled;

        return Math.Max(
            0,
            MaximumVerificationAttempts - FailedVerificationAttempts);
    }

    public void MarkExpired(DateTimeOffset now)
    {
        if (Status != WithdrawalStatus.PendingVerification)
            throw new DomainException("The withdrawal is not awaiting verification.");

        if (!IsExpired(now))
            throw new DomainException("The withdrawal code has not expired.");

        Status = WithdrawalStatus.Expired;
    }

    public void Complete(DateTimeOffset now)
    {
        if (Status != WithdrawalStatus.PendingVerification)
            throw new DomainException("This withdrawal is not awaiting verification.");

        if (IsExpired(now))
            throw new DomainException("The withdrawal code has expired.");

        Status = WithdrawalStatus.Completed;
        CompletedAt = now.ToUniversalTime();
    }
}