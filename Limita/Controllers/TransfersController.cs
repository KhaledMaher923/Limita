using Limita.Application.Common;
using Limita.Application.Features.Transfers.Commands;
using Limita.Application.Features.Transfers.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Limita.Controllers;

[ApiController]
public sealed class TransfersController(ISender sender) : ControllerBase
{
    // GET /api/beneficiaries
    [HttpGet("api/beneficiaries")]
    [Authorize]
    public async Task<IActionResult> GetBeneficiaries(CancellationToken ct)
    {
        var result = await sender.Send(new GetBeneficiariesQuery(), ct);
        return ToResponse(result);
    }

    // POST /api/beneficiaries
    [HttpPost("api/beneficiaries")]
    [Authorize]
    public async Task<IActionResult> AddBeneficiary([FromBody] AddBeneficiaryCommand command, CancellationToken ct)
    {
        var result = await sender.Send(command, ct);
        return ToResponse(result);
    }

    // GET /api/transfers/fee
    [HttpGet("api/transfers/fee")]
    public async Task<IActionResult> GetTransferFee(CancellationToken ct)
    {
        var result = await sender.Send(new GetTransferFeeQuery(), ct);
        return ToResponse(result);
    }

    // POST /api/transfers
    [HttpPost("api/transfers")]
    [Authorize]
    public async Task<IActionResult> CreateTransfer([FromBody] CreateTransferCommand command, CancellationToken ct)
    {
        var result = await sender.Send(command, ct);
        return ToResponse(result);
    }

    // GET /api/banks
    [HttpGet("api/banks")]
    public async Task<IActionResult> GetBanks(CancellationToken ct)
    {
        var result = await sender.Send(new GetBanksQuery(), ct);
        return ToResponse(result);
    }

    // GET /api/banks/{id}/branches
    [HttpGet("api/banks/{id:guid}/branches")]
    public async Task<IActionResult> GetBranches(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new GetBranchesOfBankQuery(id), ct);
        return ToResponse(result);
    }

    // ── Shared result-to-HTTP mapper ──────────────────────────────
    private IActionResult ToResponse<T>(Result<T> result)
    {
        if (result.IsSuccess)
            return Ok(result.Value);

        return result.Error.errorType switch
        {
            ErrorType.NotFound => NotFound(new { result.Error.Code, result.Error.Description }),
            ErrorType.Validation => BadRequest(new { result.Error.Code, result.Error.Description }),
            ErrorType.Unauthorized => Unauthorized(new { result.Error.Code, result.Error.Description }),
            ErrorType.Conflict => Conflict(new { result.Error.Code, result.Error.Description }),
            ErrorType.BusinessRule => UnprocessableEntity(new { result.Error.Code, result.Error.Description }),
            _ => BadRequest(new { result.Error.Code, result.Error.Description })
        };
    }
}
