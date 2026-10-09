using Limita.Domain.Common;

namespace Limita.Domain.Entities;

public sealed class ExchangeRate : Entity
{
    private ExchangeRate() { }

    public string BaseCurrency { get; private set; } = string.Empty;
    public string QuoteCurrency { get; private set; } = string.Empty;
    public decimal Rate { get; private set; }
    public decimal? BuyRate { get; private set; }
    public decimal? SellRate { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public bool IsActive { get; private set; }

    public static ExchangeRate Create(string baseCurrency, string quoteCurrency, decimal rate,
        decimal? buyRate, decimal? sellRate, DateTimeOffset updatedAt) => new()
    {
        BaseCurrency = Guard.NotBlank(baseCurrency, "Base currency", 3).ToUpperInvariant(),
        QuoteCurrency = Guard.NotBlank(quoteCurrency, "Quote currency", 3).ToUpperInvariant(),
        Rate = rate > 0 ? rate : throw new ArgumentOutOfRangeException(nameof(rate)),
        BuyRate = buyRate,
        SellRate = sellRate,
        UpdatedAt = updatedAt,
        IsActive = true
    };
}
