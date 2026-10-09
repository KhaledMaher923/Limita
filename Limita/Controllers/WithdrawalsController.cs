using Limita.Api.Common;
using Limita.Application.Features.Withdrawals.Commands;
using Limita.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Limita.Controllers;

[Authorize]
[Route("api/withdrawals")]
public sealed class WithdrawalsController(ISender sender)
    : ApiControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(
        CreateWithdrawalCommand command,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
            return FromResult(result);

        return Accepted(new { id = result.Value });
    }

    [HttpPost("{withdrawalId:guid}/confirm")]
    public async Task<IActionResult> Confirm(
        Guid withdrawalId,
        ConfirmWithdrawalRequest request,
        CancellationToken cancellationToken)
    {
        var command = new ConfirmWithdrawalCommand(
      withdrawalId,
      request.Code);

        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
            return FromResult(result);

        if (!result.Value.IsConfirmed)
        {
            return BadRequest(new
            {
                error = result.Value.ErrorMessage,
                attemptsRemaining = result.Value.AttemptsRemaining
            });
        }

        return Ok(new
        {
            withdrawalId,
            confirmed = true
        });
    }
}

public sealed record ConfirmWithdrawalRequest(string Code);
