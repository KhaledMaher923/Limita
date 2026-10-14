using Limita.Api.Common;
using Limita.Application.Features.Messages.GetMessages;
using Limita.Application.Features.Messages.GetMessageThread;
using Limita.Application.Features.Messages.SendMessageReply;
using Limita.Application.Features.Messages.StartMessageThread;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Limita.Controllers;
[Route("api/messages")]
public sealed class MessagesController(ISender sender) : ApiControllerBase
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
        return FromResult(result, id => CreatedAtAction(nameof(GetThread), new { threadId = id }, new { id }));
    }

    [HttpPost("{threadId:guid}/replies")]
    public async Task<IActionResult> Reply(Guid threadId, [FromBody] ReplyRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new SendMessageReplyCommand(threadId, request.Body), ct);
        return FromResult(result, id => StatusCode(StatusCodes.Status201Created, new { id }));
    }
}
