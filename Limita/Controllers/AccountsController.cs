using Limita.Application.Common.Messaging;
using Limita.Application.Features.Accounts;
using Limita.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Limita.Controllers;

[Authorize]
[Route("api/accounts")]
public sealed class AccountsController(ISender sender) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAccounts(CancellationToken cancellationToken)
        => ToActionResult(await sender.Send(new GetAccounts.Query(), cancellationToken));

    [HttpGet("{id:guid}/balance")]
    public async Task<IActionResult> GetBalance(Guid id, CancellationToken cancellationToken)
        => ToActionResult(await sender.Send(new GetAccountBalanceQuery(id), cancellationToken));

    [HttpPost("{id:guid}/balance/debit")]
    public async Task<IActionResult> Debit(Guid id, BalanceAdjustmentRequest request, CancellationToken cancellationToken)
        => ToActionResult(await sender.Send(new AdjustAccountBalanceCommand(id, request.Amount, false), cancellationToken));

    [HttpPost]
    public async Task<IActionResult> Create(CreateAccount.Command command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        if (result.IsFailure) return ToActionResult(result);
        return StatusCode(StatusCodes.Status201Created, result.Value);
    }

    public sealed record BalanceAdjustmentRequest(decimal Amount);
}
