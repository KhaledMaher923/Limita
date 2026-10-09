using Limita.Application.Features.MobilePrepaid.Commands;
using Limita.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Limita.Controllers;

[Authorize]
[Route("api/mobile-topups")]
public sealed class MobileTopUpsController(ISender sender) : ApiControllerBase
{
    [HttpPost]
    public async Task<IActionResult> TopUp(
        TopUpMobileCommand command,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);

        return ToActionResult(result);
    }
}
