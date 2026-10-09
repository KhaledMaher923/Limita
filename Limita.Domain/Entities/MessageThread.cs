using Limita.Domain.Common;

namespace Limita.Domain.Entities;

public sealed class MessageThread : Entity
{
    private MessageThread() { }

    public Guid UserId { get; private set; }
    public string Subject { get; private set; } = string.Empty;
    public string Status { get; private set; } = "Open";
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public static MessageThread Open(Guid userId, string subject, DateTimeOffset now) => new()
    {
        UserId = userId,
        Subject = Guard.NotBlank(subject, "Subject", 120),
        Status = "Open",
        CreatedAt = now,
        UpdatedAt = now
    };

    public void Touch(DateTimeOffset now) => UpdatedAt = now;
    public void Close(DateTimeOffset now) { Status = "Closed"; UpdatedAt = now; }
}
