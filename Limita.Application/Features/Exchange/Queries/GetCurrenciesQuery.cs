using Limita.Application.Common;
using Limita.Application.Common.Messaging;

namespace Limita.Application.Features.Exchange.Queries;

public sealed record GetCurrenciesQuery
    : IQuery<IReadOnlyList<CurrencyDto>>;

public sealed record CurrencyDto(
    string Code,
    string Name);

internal sealed class GetCurrenciesHandler
    : IQueryHandler<GetCurrenciesQuery, IReadOnlyList<CurrencyDto>>
{
    public Task<Result<IReadOnlyList<CurrencyDto>>> Handle(
        GetCurrenciesQuery request,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<CurrencyDto> currencies =
            ExchangeCurrencyCatalog.Currencies
                .Select(currency => new CurrencyDto(
                    currency.Code,
                    currency.Name))
                .ToList();

        return Task.FromResult(
            Result.Success<IReadOnlyList<CurrencyDto>>(currencies));
    }
}