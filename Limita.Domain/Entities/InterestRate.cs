using Limita.Domain.Common;

namespace Limita.Domain.Entities;

public sealed class InterestRate : Entity
{
    private InterestRate() { }

    public string ProductCode { get; private set; } = string.Empty;
    public string ProductName { get; private set; } = string.Empty;
    public decimal AnnualRatePercent { get; private set; }
    public int? MinimumTermMonths { get; private set; }
    public int? MaximumTermMonths { get; private set; }
    public decimal? MinimumAmount { get; private set; }
    public string CurrencyCode { get; private set; } = "USD";
    public DateTimeOffset EffectiveFrom { get; private set; }
    public bool IsActive { get; private set; }

    public static InterestRate Create(string productCode, string productName, decimal annualRatePercent,
        int? minimumTermMonths, int? maximumTermMonths, decimal? minimumAmount,
        string currencyCode, DateTimeOffset effectiveFrom) => new()
    {
        ProductCode = Guard.NotBlank(productCode, "Product code", 30),
        ProductName = Guard.NotBlank(productName, "Product name", 100),
        AnnualRatePercent = annualRatePercent >= 0 ? annualRatePercent : throw new ArgumentOutOfRangeException(nameof(annualRatePercent)),
        MinimumTermMonths = minimumTermMonths,
        MaximumTermMonths = maximumTermMonths,
        MinimumAmount = minimumAmount,
        CurrencyCode = Guard.NotBlank(currencyCode, "Currency code", 3).ToUpperInvariant(),
        EffectiveFrom = effectiveFrom,
        IsActive = true
    };
}
