using Limita.Application.Common.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Limita.Application.Features.ReferenceData.GetReferenceData;

public sealed record GetBranchesQuery : IRequest<IReadOnlyList<BranchDto>>;
public sealed record GetInterestRatesQuery : IRequest<IReadOnlyList<InterestRateDto>>;
public sealed record GetExchangeRatesQuery : IRequest<IReadOnlyList<ExchangeRateDto>>;
public sealed record GetLanguagesQuery : IRequest<IReadOnlyList<LanguageDto>>;
public sealed record BranchDto(Guid Id, string Name, string Address, string City, string? PhoneNumber, decimal Latitude, decimal Longitude);
public sealed record InterestRateDto(Guid Id, string ProductCode, string ProductName, decimal AnnualRatePercent, int? MinimumTermMonths, int? MaximumTermMonths, decimal? MinimumAmount, string CurrencyCode, DateTimeOffset EffectiveFrom);
public sealed record ExchangeRateDto(Guid Id, string BaseCurrency, string QuoteCurrency, decimal Rate, decimal? BuyRate, decimal? SellRate, DateTimeOffset UpdatedAt);
public sealed record LanguageDto(Guid Id, string Code, string NativeName, string EnglishName, bool IsRightToLeft);

public sealed class GetBranchesHandler(IApplicationDbContext db) : IRequestHandler<GetBranchesQuery, IReadOnlyList<BranchDto>>
{
    public async Task<IReadOnlyList<BranchDto>> Handle(GetBranchesQuery request, CancellationToken ct) =>
        await db.Branches.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.City).ThenBy(x => x.Name)
            .Select(x => new BranchDto(x.Id, x.Name, x.Address, x.City, x.PhoneNumber, x.Latitude, x.Longitude)).ToListAsync(ct);
}
public sealed class GetInterestRatesHandler(IApplicationDbContext db) : IRequestHandler<GetInterestRatesQuery, IReadOnlyList<InterestRateDto>>
{
    public async Task<IReadOnlyList<InterestRateDto>> Handle(GetInterestRatesQuery request, CancellationToken ct) =>
        await db.InterestRates.AsNoTracking().Where(x => x.IsActive && x.EffectiveFrom <= DateTimeOffset.UtcNow)
            .OrderBy(x => x.ProductName).Select(x => new InterestRateDto(x.Id, x.ProductCode, x.ProductName, x.AnnualRatePercent, x.MinimumTermMonths, x.MaximumTermMonths, x.MinimumAmount, x.CurrencyCode, x.EffectiveFrom)).ToListAsync(ct);
}
public sealed class GetExchangeRatesHandler(IApplicationDbContext db) : IRequestHandler<GetExchangeRatesQuery, IReadOnlyList<ExchangeRateDto>>
{
    public async Task<IReadOnlyList<ExchangeRateDto>> Handle(GetExchangeRatesQuery request, CancellationToken ct) =>
        await db.ExchangeRates.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.BaseCurrency).ThenBy(x => x.QuoteCurrency)
            .Select(x => new ExchangeRateDto(x.Id, x.BaseCurrency, x.QuoteCurrency, x.Rate, x.BuyRate, x.SellRate, x.UpdatedAt)).ToListAsync(ct);
}
public sealed class GetLanguagesHandler(IApplicationDbContext db) : IRequestHandler<GetLanguagesQuery, IReadOnlyList<LanguageDto>>
{
    public async Task<IReadOnlyList<LanguageDto>> Handle(GetLanguagesQuery request, CancellationToken ct) =>
        await db.Languages.AsNoTracking().Where(x => x.IsEnabled).OrderBy(x => x.Code)
            .Select(x => new LanguageDto(x.Id, x.Code, x.NativeName, x.EnglishName, x.IsRightToLeft)).ToListAsync(ct);
}
