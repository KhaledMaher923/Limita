using Limita.Api.Common;
using Limita.Application.Features.Transfers.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Limita.Api.Controllers;

/// <summary>Reference data used by the transfer screen. Public, like the branch locator.</summary>
[AllowAnonymous]
[Route("api/banks")]
public sealed class BanksController(ISender sender) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetBanks(CancellationToken ct)
        => FromResult(await sender.Send(new GetBanksQuery(), ct));

    [HttpGet("{id:guid}/branches")]
    public async Task<IActionResult> GetBranches(Guid id, CancellationToken ct)
    => FromResult(await sender.Send(new GetBranchesOfBankQuery(id), ct));

}

