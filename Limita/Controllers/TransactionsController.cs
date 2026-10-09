using Limita.Application.Features.Transactions.GetTransactionReport;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Limita.Controllers;

[ApiController]
[Authorize]
[Route("api/transactions")]
public sealed class TransactionsController(ISender sender) : ControllerBase
{
    /// <summary>Returns the authenticated user's ledger entries for a calendar month (YYYY-MM).</summary>
    [HttpGet]
    public async Task<IActionResult> GetReport([FromQuery] string month, CancellationToken ct)
    {
        if (!DateOnly.TryParseExact(month, "yyyy-MM", out var parsed))
            return BadRequest(new { message = "month must use YYYY-MM format." });
        var result = await sender.Send(new GetTransactionReportQuery(parsed.Year, parsed.Month), ct);
        return Ok(result);
    }
}
