using Limita.Application.Features.Messages.GetMessages;
using Limita.Application.Features.Messages.GetMessageThread;
using Limita.Application.Features.Messages.SendMessageReply;
using Limita.Application.Features.Messages.StartMessageThread;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Limita.Controllers;

[ApiController]
[Authorize]
[Route("api/messages")]
public sealed class MessagesController(ISender sender) : ControllerBase
{
    public sealed record StartThreadRequest(string Subject, string Body);
    public sealed record ReplyRequest(string Body);

    [HttpGet]
    public async Task<IActionResult> GetThreads(CancellationToken ct) => Ok(await sender.Send(new GetMessagesQuery(), ct));

    [HttpGet("{threadId:guid}")]
    public async Task<IActionResult> GetThread(Guid threadId, CancellationToken ct)
    {
        var thread = await sender.Send(new GetMessageThreadQuery(threadId), ct);
        return thread is null ? NotFound() : Ok(thread);
    }

    [HttpPost]
    public async Task<IActionResult> StartThread([FromBody] StartThreadRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new StartMessageThreadCommand(request.Subject, request.Body), ct);
        if (result.IsFailure) return BadRequest(new { result.Error.Code, result.Error.Description });
        return CreatedAtAction(nameof(GetThread), new { threadId = result.Value }, new { id = result.Value });
    }

    [HttpPost("{threadId:guid}/replies")]
    public async Task<IActionResult> Reply(Guid threadId, [FromBody] ReplyRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new SendMessageReplyCommand(threadId, request.Body), ct);
        if (result.IsFailure) return result.Error.Type == Limita.Application.Common.ErrorType.Conflict
            ? Conflict(new { result.Error.Code, result.Error.Description })
            : NotFound(new { result.Error.Code, result.Error.Description });
        return StatusCode(StatusCodes.Status201Created, new { id = result.Value });
    }
}
