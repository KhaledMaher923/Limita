using Limita.Application.Common;
using Limita.Application.Features.CreditCards;
using Limita.Application.Features.CreditCards.Commands;
using Limita.Application.Features.CreditCards.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Limita.Controllers;

[ApiController]
[Route("api/credit-cards")]
[Authorize]
public sealed class CreditCardsController : ControllerBase
{
    private readonly IMediator _mediator;

    public CreditCardsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("{cardId:guid}/statement")]
    public async Task<ActionResult<Result<CreditCardStatementDto>>> GetStatement(Guid cardId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetCreditCardStatementQuery(cardId), cancellationToken);
        return result.IsSuccess ? Ok(result) : HandleFailure(result);
    }

    [HttpPost("{cardId:guid}/repay")]
    public async Task<ActionResult<Result<RepayCreditCardResponse>>> Repay(Guid cardId, [FromBody] RepayCreditCardRequest request, CancellationToken cancellationToken)
    {
        var repayRequest = request with { CardId = cardId };
        var result = await _mediator.Send(new RepayCreditCardCommand(repayRequest), cancellationToken);
        return result.IsSuccess ? Ok(result) : HandleFailure(result);
    }

    [HttpPost("{cardId:guid}/validate-repayment")]
    public async Task<ActionResult<Result<ValidateRepaymentResponse>>> ValidateRepayment(Guid cardId, [FromBody] ValidateRepaymentRequest request, CancellationToken cancellationToken)
    {
        var validateRequest = request with { CardId = cardId };
        var result = await _mediator.Send(new ValidateRepaymentQuery(validateRequest), cancellationToken);
        return result.IsSuccess ? Ok(result) : HandleFailure(result);
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
}