using Limita.Application.Features.Exchange.Commands;
using Limita.Application.Features.Exchange.Queries;
using Limita.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Limita.Controllers;

[Authorize]
[Route("api/exchange")]
public sealed class ExchangeController(ISender sender)
    : ApiControllerBase
{
    [AllowAnonymous]
    [HttpGet("currencies")]
    public async Task<IActionResult> GetCurrencies(
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new GetCurrenciesQuery(),
            cancellationToken);

        return ToActionResult(result);
    }

    [AllowAnonymous]
    [HttpGet("quote")]
    public async Task<IActionResult> GetQuote(
        [FromQuery] string fromCurrency,
        [FromQuery] string toCurrency,
        [FromQuery] decimal amount,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new GetExchangeQuoteQuery(
                fromCurrency,
                toCurrency,
                amount),
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> Exchange(
        ExchangeCurrencyCommand command,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
            return ToActionResult(result);

        return Ok(new { exchangeOperationId = result.Value });
    }
}
