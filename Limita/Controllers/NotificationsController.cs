using Limita.Application.Features.Notifications.GetNotifications;
using Limita.Application.Features.Notifications.MarkNotificationRead;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Limita.Controllers;

[ApiController]
[Authorize]
[Route("api/notifications")]
public sealed class NotificationsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] bool unreadOnly = false, CancellationToken ct = default) =>
        Ok(await sender.Send(new GetNotificationsQuery(unreadOnly), ct));

    [HttpPatch("{id:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new MarkNotificationReadCommand(id), ct);
        return result.IsSuccess ? NoContent() : NotFound(new { result.Error.Code, result.Error.Description });
    }
}
