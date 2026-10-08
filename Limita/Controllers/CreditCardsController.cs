using Limita.Application.Common.Messaging;
using Limita.Application.Features.Accounts;
using Limita.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Limita.Controllers;

[Authorize]
[Route("api/credit-cards")]
public sealed class CreditCardsController(ISender sender) : ApiControllerBase
{
    [HttpGet("{id:guid}/statement")]
    public async Task<IActionResult> GetStatement(Guid id, CancellationToken cancellationToken)
        => ToActionResult(await sender.Send(new GetCreditCardStatementQuery(id), cancellationToken));

    [HttpPost("{id:guid}/repay")]
    public async Task<IActionResult> Repay(Guid id, RepaymentRequest request, CancellationToken cancellationToken)
        => ToActionResult(await sender.Send(new RepayCreditCardCommand(id, request.SourceAccountId, request.Amount), cancellationToken));

    public sealed record RepaymentRequest(Guid SourceAccountId, decimal Amount);
}
