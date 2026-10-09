namespace Limita.Application.Features.Exchange;

public sealed record CurrencyDefinition(
    string Code,
    string Name,
    decimal EGPPerUnit);

public static class ExchangeCurrencyCatalog
{
    // Temporary fictional development rates.
    // EGPPerUnit means how many EGP equal one unit of that currency.
    public static readonly IReadOnlyList<CurrencyDefinition> Currencies =
    [
        new("EGP", "Egyptian Pound", 1m),
        new("USD", "US Dollar", 50m),
        new("EUR", "Euro", 54m),
        new("GBP", "British Pound", 64m),
        new("SAR", "Saudi Riyal", 13.33m),
        new("AED", "UAE Dirham", 13.61m)
    ];

    public static CurrencyDefinition? Find(string code)
        => Currencies.FirstOrDefault(
            currency => string.Equals(
                currency.Code,
                code,
                StringComparison.OrdinalIgnoreCase));

    public static decimal Convert(
        decimal amount,
        CurrencyDefinition from,
        CurrencyDefinition to)
    {
        var valueInEgp = amount * from.EGPPerUnit;

        return decimal.Round(
            valueInEgp / to.EGPPerUnit,
            2,
            MidpointRounding.AwayFromZero);
    }
}
