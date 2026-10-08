using Limita.Domain.Common;
using Limita.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Limita.Domain.Entities
{
    public sealed class Notification : Entity
    {
        private Notification() // required by EF Core
        {
        }

        public Guid UserId { get; private set; }
        public NotificationType Type { get; private set; }
        public string Title { get; private set; } = string.Empty;
        public string Body { get; private set; } = string.Empty;
        public bool IsRead { get; private set; }
        public Guid? RelatedEntityId { get; private set; }
        public DateTimeOffset CreatedAt { get; private set; }

        public static Notification Create(
            Guid userId,
            NotificationType type,
            string title,
            string body,
            Guid? relatedEntityId,
            DateTimeOffset now) => new()
        {
            UserId = userId,
            Type = type,
            Title = Guard.NotBlank(title, "Title", 100),
            Body = Guard.NotBlank(body, "Body", 500),
            RelatedEntityId = relatedEntityId,
            CreatedAt = now
        };

        public void MarkAsRead() => IsRead = true;


    }
}
