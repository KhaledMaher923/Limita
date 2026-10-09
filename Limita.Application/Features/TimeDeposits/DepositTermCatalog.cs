namespace Limita.Application.Features.TimeDeposits;

public sealed record DepositTerm(
    int TermInMonths,
    decimal AnnualInterestRatePercent);

public static class DepositTermCatalog
{
    public const decimal MinimumPrincipal = 1000m;

    public static readonly IReadOnlyList<DepositTerm> Terms =
    [
        new(3, 4.00m),
        new(6, 4.50m),
        new(12, 5.00m),
        new(16, 5.50m),
        new(24, 6.00m)
    ];

    public static DepositTerm? Find(int termInMonths)
        => Terms.FirstOrDefault(term => term.TermInMonths == termInMonths);
}