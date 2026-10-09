using Limita.Api.Common;
using Limita.Application.Features.Bills.Commands;
using Limita.Application.Features.Bills.Queries;
using Limita.Common;
using Limita.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Limita.Controllers;

[Authorize]
[Route("api/bills")]
public sealed class BillsController(ISender sender) : ApiControllerBase
{
    [HttpGet("lookup")]
    public async Task<IActionResult> Lookup(
        [FromQuery] BillType type,
        [FromQuery] string code,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetBillByCodeQuery(type, code), cancellationToken);

        return FromResult(result);
    }

    [HttpGet("due")]
    public async Task<IActionResult> GetDue(
        [FromQuery] int daysAhead = 7,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetDueBillsQuery(daysAhead), cancellationToken);

        return FromResult(result);
    }

    [HttpGet("payments")]
    public async Task<IActionResult> GetPayments(
        [FromQuery] BillType? type,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(
            new GetPaymentHistoryQuery(type, page, pageSize),
            cancellationToken);

        return FromResult(result);
    }

    [HttpPost("pay")]
    public async Task<IActionResult> Pay(
        PayBillCommand command,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);

        return FromResult(result);
    }

    [HttpPost("smart-pay")]
    public async Task<IActionResult> SmartPay(
        SmartPayBillsCommand command,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);

        return FromResult(result);
    }
}
