using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Limita.Domain.Common
{
    public abstract class Entity
    {
        private readonly List<IDomainEvent> _domainEvents = [];

        public Guid Id { get; protected set; } = Guid.NewGuid();

        [NotMapped]
        public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

        protected void Raise(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

        public void ClearDomainEvents() => _domainEvents.Clear();

    }
}
