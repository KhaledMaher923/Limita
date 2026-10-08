using Limita.Domain.Common;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Limita.Application.Common.DomainEvents
{
    /// <summary>
    /// Wraps a domain event so it can travel through MediatR without the Domain project depending on MediatR.
    /// Handle events with: INotificationHandler&lt;DomainEventNotification&lt;TransactionCreated&gt;&gt;.
    /// </summary>
    public sealed record DomainEventNotification<TEvent>(TEvent DomainEvent) : INotification
        where TEvent : IDomainEvent;

    public static class DomainEventNotification
    {
        public static INotification Create(IDomainEvent domainEvent)
        {
            var type = typeof(DomainEventNotification<>).MakeGenericType(domainEvent.GetType());
            return (INotification)Activator.CreateInstance(type, domainEvent)!;
        }
    }
}
