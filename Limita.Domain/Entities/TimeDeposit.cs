using Limita.Domain.Common;
using Limita.Domain.Enums;
using Limita.Domain.ValueObjects;
using System;   

namespace Limita.Domain.Entities;

public sealed class TimeDeposit : Entity
{
    private TimeDeposit()
    {
        // EF Core needs a private constructor to load the entity from the database.
    }

    public Guid UserId { get; private set; }
    public Guid FundingAccountId { get; private set; }
    public Money Principal { get; private set; } = null!;
    public int TermInMonths { get; private set; }
    public decimal AnnualInterestRatePercent { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset MaturesAt { get; private set; }
    public TimeDepositStatus Status { get; private set; }

    public static TimeDeposit Create(
        Guid userId,
        Guid fundingAccountId,
        Money principal,
        int termInMonths,
        decimal annualInterestRatePercent,
        DateTimeOffset createdAt)
    {
        if (userId == Guid.Empty)
            throw new DomainException("User is required.");

        if (fundingAccountId == Guid.Empty)
            throw new DomainException("Funding account is required.");

        Guard.Positive(principal, "Deposit amount");

        if (principal.Amount < 1000m)
            throw new DomainException("The minimum time deposit is 1000.");

        if (termInMonths is not (3 or 6 or 12 or 16 or 24))
            throw new DomainException("The selected deposit term is not supported.");

        if (annualInterestRatePercent <= 0m || annualInterestRatePercent > 100m)
            throw new DomainException("The annual interest rate is invalid.");

        var utcCreatedAt = createdAt.ToUniversalTime();

        return new TimeDeposit
        {
            UserId = userId,
            FundingAccountId = fundingAccountId,
            Principal = principal,
            TermInMonths = termInMonths,
            AnnualInterestRatePercent = annualInterestRatePercent,
            CreatedAt = utcCreatedAt,
            MaturesAt = utcCreatedAt.AddMonths(termInMonths),
            Status = TimeDepositStatus.Active
        };
    }

    public Money CalculateMaturityAmount()
    {
        var interestAmount = decimal.Round(
            Principal.Amount
                * (AnnualInterestRatePercent / 100m)
                * (TermInMonths / 12m),
            2,
            MidpointRounding.AwayFromZero);

        return Money.Of(
            Principal.Amount + interestAmount,
            Principal.Currency);
    }

    public Money Redeem(DateTimeOffset now)
    {
        if (Status == TimeDepositStatus.Redeemed)
            throw new DomainException("This time deposit has already been redeemed.");

        if (now.ToUniversalTime() < MaturesAt)
            throw new DomainException("The time deposit has not matured yet.");

        Status = TimeDepositStatus.Matured;

        var amountToPay = CalculateMaturityAmount();

        Status = TimeDepositStatus.Redeemed;

        return amountToPay;
    }
}
