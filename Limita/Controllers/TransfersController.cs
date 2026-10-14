using Limita.Api.Common;
using Limita.Application.Common;
using Limita.Application.Features.Transfers.Commands;
using Limita.Application.Features.Transfers.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Limita.Controllers;

[Route("api/transfers")]
public sealed class TransfersController(ISender sender) : ApiControllerBase
{
    /// <summary>GET /api/transfers/fee: the flat fee charged on every transfer to another bank or card. Public.</summary>
    [AllowAnonymous]
    [HttpGet("fee")]
    public async Task<IActionResult> GetTransferFee(CancellationToken ct)
        => FromResult(await sender.Send(new GetTransferFeeQuery(), ct));

    /// <summary>POST /api/transfers: sends money to a beneficiary. Send an IdempotencyKey so a retry never charges twice.</summary>
    [HttpPost]
    public async Task<IActionResult> CreateTransfer([FromBody] CreateTransferCommand command, CancellationToken ct)
        => FromResult(await sender.Send(command, ct));
    
}
