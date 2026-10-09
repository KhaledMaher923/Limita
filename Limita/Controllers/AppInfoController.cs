using Limita.Application.Features.AppInfo.GetAppInformation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Limita.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/app-info")]
public sealed class AppInfoController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct) => Ok(await sender.Send(new GetAppInformationQuery(), ct));
}
