using Limita.Application.Features.SpendingLimits;
using Limita.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Limita.Controllers;

[Authorize]
[Route("api/spending-limits")]
public sealed class SpendingLimitsController(ISender sender)
    : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new GetSpendingLimitsQuery(),
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> Set(
        SetSpendingLimitCommand command,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
            return ToActionResult(result);

        return StatusCode(
            StatusCodes.Status201Created,
            new { id = result.Value });
    }
}