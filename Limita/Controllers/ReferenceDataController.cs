using Limita.Application.Features.ReferenceData.GetReferenceData;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Limita.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api")]
public sealed class ReferenceDataController(ISender sender) : ControllerBase
{
    [HttpGet("branches")]
    public async Task<IActionResult> GetBranches(CancellationToken ct) => Ok(await sender.Send(new GetBranchesQuery(), ct));
    [HttpGet("interest-rates")]
    public async Task<IActionResult> GetInterestRates(CancellationToken ct) => Ok(await sender.Send(new GetInterestRatesQuery(), ct));
    [HttpGet("exchange-rates")]
    public async Task<IActionResult> GetExchangeRates(CancellationToken ct) => Ok(await sender.Send(new GetExchangeRatesQuery(), ct));
    [HttpGet("languages")]
    public async Task<IActionResult> GetLanguages(CancellationToken ct) => Ok(await sender.Send(new GetLanguagesQuery(), ct));
}
