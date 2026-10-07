using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Limita.Domain.Common
{
    public interface IDomainEvent
    {
        DateTimeOffset OccurredOn { get; }
    }
    public abstract record DomainEvent : IDomainEvent
    {
        public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
    }

}
