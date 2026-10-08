using Limita.Application.Common.Messaging;
using Limita.Application.Features.Accounts;
using Limita.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Limita.Controllers;

[Authorize]
[Route("api/cards")]
public sealed class CardsController(ISender sender) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetCards(CancellationToken cancellationToken)
        => ToActionResult(await sender.Send(new GetCardsQuery(), cancellationToken));

    [HttpPost]
    public async Task<IActionResult> AddCard(AddCardCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        if (result.IsFailure) return ToActionResult(result);
        return StatusCode(StatusCodes.Status201Created, result.Value);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetCard(Guid id, CancellationToken cancellationToken)
        => ToActionResult(await sender.Send(new GetCardQuery(id), cancellationToken));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteCard(Guid id, CancellationToken cancellationToken)
        => ToActionResult(await sender.Send(new DeleteCardCommand(id), cancellationToken));
}
