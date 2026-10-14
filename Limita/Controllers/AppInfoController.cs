using Limita.Api.Common;
using Limita.Application.Features.AppInfo.GetAppInformation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Limita.Controllers;

[AllowAnonymous]
[Route("api/app-info")]
public sealed class AppInfoController(ISender sender) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct) => Ok(await sender.Send(new GetAppInformationQuery(), ct));
}
