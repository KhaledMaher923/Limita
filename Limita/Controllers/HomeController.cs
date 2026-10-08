using Limita.Application.Features.Accounts;
using Limita.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Limita.Controllers;

[Authorize]
[Route("api/home")]
public sealed class HomeController(ISender sender) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
        => ToActionResult(await sender.Send(new GetHomeDashboardQuery(), cancellationToken));
}
