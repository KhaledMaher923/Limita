using Limita.Application.Common;
using Limita.Application.Features.Home;
using Limita.Application.Features.Home.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Limita.Controllers;

[ApiController]
[Route("api/home")]
[Authorize]
public sealed class HomeController : ControllerBase
{
    private readonly IMediator _mediator;

    public HomeController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<Result<HomeDashboardDto>>> GetDashboard(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetHomeDashboardQuery(), cancellationToken);
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