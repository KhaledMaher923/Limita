using Limita.Application.Common.Messaging;
using Limita.Application.Features.TimeDeposits.Commands;
using Limita.Application.Features.TimeDeposits.Queries;
using Limita.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Limita.Controllers;

[Route("api/time-deposits")]
public sealed class TimeDepositsController(ISender sender)
    : ApiControllerBase
{
    [AllowAnonymous]
    [HttpGet("terms")]
    public async Task<IActionResult> GetTerms(
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new GetTimeDepositTermsQuery(),
            cancellationToken);

        return ToActionResult(result);
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> GetMyDeposits(
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new GetTimeDepositsQuery(),
            cancellationToken);

        return ToActionResult(result);
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create(
        CreateTimeDepositCommand command,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
            return ToActionResult(result);

        return StatusCode(
            StatusCodes.Status201Created,
            new { id = result.Value });
    }

    [Authorize]
    [HttpPost("{id:guid}/redeem")]
    public async Task<IActionResult> Redeem(
    Guid id,
    CancellationToken cancellationToken)
    {
        var command = new RedeemTimeDepositCommand(id);

        var result = await sender.Send(command, cancellationToken);

        return ToActionResult(result);
    }
}