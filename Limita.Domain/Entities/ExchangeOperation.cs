using Limita.Domain.Common;
using Limita.Domain.ValueObjects;


namespace Limita.Domain.Entities;

public sealed class ExchangeOperation : Entity
{
    private ExchangeOperation()
    {
    }

    public Guid UserId { get; private set; }
    public Guid SourceAccountId { get; private set; }
    public Guid TargetAccountId { get; private set; }
    public Money SourceAmount { get; private set; } = null!;
    public Money TargetAmount { get; private set; } = null!;
    public decimal ExchangeRate { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public static ExchangeOperation Create(
        Guid userId,
        Guid sourceAccountId,
        Guid targetAccountId,
        Money sourceAmount,
        Money targetAmount,
        decimal exchangeRate,
        DateTimeOffset createdAt)
    {
        if (userId == Guid.Empty)
            throw new DomainException("User is required.");

        if (sourceAccountId == targetAccountId)
            throw new DomainException("Exchange requires two different accounts.");

        Guard.Positive(sourceAmount, "Source amount");
        Guard.Positive(targetAmount, "Target amount");

        if (sourceAmount.Currency == targetAmount.Currency)
            throw new DomainException("Source and target currencies must differ.");

        if (exchangeRate <= 0m)
            throw new DomainException("Exchange rate must be positive.");

        return new ExchangeOperation
        {
            UserId = userId,
            SourceAccountId = sourceAccountId,
            TargetAccountId = targetAccountId,
            SourceAmount = sourceAmount,
            TargetAmount = targetAmount,
            ExchangeRate = exchangeRate,
            CreatedAt = createdAt.ToUniversalTime()
        };
    }
}