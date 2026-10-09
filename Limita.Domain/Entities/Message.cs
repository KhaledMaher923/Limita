using Limita.Domain.Common;

namespace Limita.Domain.Entities;

public sealed class Message : Entity
{
    private Message() { }

    public Guid ThreadId { get; private set; }
    public Guid SenderUserId { get; private set; }
    public string Body { get; private set; } = string.Empty;
    public bool IsFromBank { get; private set; }
    public DateTimeOffset SentAt { get; private set; }

    public static Message Send(Guid threadId, Guid senderUserId, string body, bool isFromBank, DateTimeOffset now) => new()
    {
        ThreadId = threadId,
        SenderUserId = senderUserId,
        Body = Guard.NotBlank(body, "Message body", 2000),
        IsFromBank = isFromBank,
        SentAt = now
    };
}
