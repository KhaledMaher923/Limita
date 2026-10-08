using Limita.Application.Common;
using Limita.Application.Features.Cards;
using Limita.Application.Features.Cards.Commands;
using Limita.Application.Features.Cards.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Limita.Controllers;

[ApiController]
[Route("api/cards")]
[Authorize]
public sealed class CardsController : ControllerBase
{
    private readonly IMediator _mediator;

    public CardsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<Result<List<CardDto>>>> GetCards(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetCardsQuery(), cancellationToken);
        return result.IsSuccess ? Ok(result) : HandleFailure(result);
    }

    [HttpGet("{cardId:guid}")]
    public async Task<ActionResult<Result<CardDto>>> GetCard(Guid cardId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetCardByIdQuery(cardId), cancellationToken);
        return result.IsSuccess ? Ok(result) : HandleFailure(result);
    }

    [HttpPost]
    public async Task<ActionResult<Result<AddCardResponse>>> AddCard([FromBody] AddCardRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new AddCardCommand(request), cancellationToken);
        return result.IsSuccess ? CreatedAtAction(nameof(GetCard), new { cardId = result.Value.CardId }, result) : HandleFailure(result);
    }

    [HttpDelete("{cardId:guid}")]
    public async Task<ActionResult<Result>> DeleteCard(Guid cardId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new DeleteCardCommand(cardId), cancellationToken);
        return result.IsSuccess ? NoContent() : HandleFailure(result);
    }

private ActionResult HandleFailure<T>(Result<T> result)
        {
            return result.Error.errorType switch
            {
                ErrorType.NotFound => NotFound(result.Error),
                ErrorType.Unauthorized => Unauthorized(result.Error),
                ErrorType.Forbidden => Forbid(),
                ErrorType.Validation => BadRequest(result.Error),
                ErrorType.BusinessRule => Conflict(result.Error),
                _ => StatusCode(500, result.Error)
            };
        }

    private ActionResult HandleFailure(Result result)
    {
        return result.Error.errorType switch
        {
            ErrorType.NotFound => NotFound(result.Error),
            ErrorType.Unauthorized => Unauthorized(result.Error),
            ErrorType.Forbidden => Forbid(),
            ErrorType.Validation => BadRequest(result.Error),
            ErrorType.BusinessRule => Conflict(result.Error),
            _ => StatusCode(500, result.Error)
        };
    }
}