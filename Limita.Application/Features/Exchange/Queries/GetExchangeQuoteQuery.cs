using FluentValidation;
using Limita.Application.Common;
using Limita.Application.Common.Messaging;

namespace Limita.Application.Features.Exchange.Queries;

public sealed record GetExchangeQuoteQuery(
    string FromCurrency,
    string ToCurrency,
    decimal Amount) : IQuery<ExchangeQuoteDto>;

public sealed record ExchangeQuoteDto(
    string FromCurrency,
    string ToCurrency,
    decimal SourceAmount,
    decimal ExchangeRate,
    decimal TargetAmount);

public sealed class GetExchangeQuoteValidator
    : AbstractValidator<GetExchangeQuoteQuery>
{
    public GetExchangeQuoteValidator()
    {
        RuleFor(query => query.Amount)
            .GreaterThan(0);

        RuleFor(query => query.FromCurrency)
            .Must(code => ExchangeCurrencyCatalog.Find(code) is not null)
            .WithMessage("The source currency is not supported.");

        RuleFor(query => query.ToCurrency)
            .Must(code => ExchangeCurrencyCatalog.Find(code) is not null)
            .WithMessage("The target currency is not supported.");

        RuleFor(query => query)
            .Must(query => !string.Equals(
                query.FromCurrency,
                query.ToCurrency,
                StringComparison.OrdinalIgnoreCase))
            .WithMessage("Source and target currencies must be different.");
    }
}

internal sealed class GetExchangeQuoteHandler
    : IQueryHandler<GetExchangeQuoteQuery, ExchangeQuoteDto>
{
    public Task<Result<ExchangeQuoteDto>> Handle(
        GetExchangeQuoteQuery request,
        CancellationToken cancellationToken)
    {
        var source = ExchangeCurrencyCatalog.Find(request.FromCurrency);
        var target = ExchangeCurrencyCatalog.Find(request.ToCurrency);

        if (source is null || target is null)
        {
            return Task.FromResult(
                Result.Failure<ExchangeQuoteDto>(
                    Error.Validation(
                        "Exchange.Currency.Unsupported",
                        "One of the currencies is not supported.")));
        }

        var convertedAmount = ExchangeCurrencyCatalog.Convert(
            request.Amount,
            source,
            target);

        var rate = ExchangeCurrencyCatalog.Convert(
            1m,
            source,
            target);

        var quote = new ExchangeQuoteDto(
            source.Code,
            target.Code,
            request.Amount,
            rate,
            convertedAmount);

        return Task.FromResult(Result.Success(quote));
    }
}