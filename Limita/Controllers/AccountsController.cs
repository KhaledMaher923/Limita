using Limita.Application.Common;
using Limita.Application.Features.Accounts;
using Limita.Application.Features.Accounts.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Limita.Controllers;

[ApiController]
[Route("api/accounts")]
[Authorize]
public sealed class AccountsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AccountsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<Result<List<AccountDto>>>> GetAccounts(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetAccountsQuery(), cancellationToken);
        return result.IsSuccess ? Ok(result) : HandleFailure(result);
    }

    [HttpGet("{accountId:guid}/balance")]
    public async Task<ActionResult<Result<AccountBalanceDto>>> GetBalance(Guid accountId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetAccountBalanceQuery(accountId), cancellationToken);
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