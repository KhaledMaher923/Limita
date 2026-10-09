using Limita.Domain.Common;

namespace Limita.Domain.Entities
{
    public sealed class BankBranch : Entity
    {
        private BankBranch() // required by EF Core
        {
        }

        public Guid BankId { get; private set; }
        public string Name { get; private set; } = string.Empty;
        public string Address { get; private set; } = string.Empty;
        public DateTimeOffset CreatedAt { get; private set; }

        public static BankBranch Create(Guid bankId, string name, string address, DateTimeOffset now)
        {
            if (bankId == Guid.Empty)
                throw new DomainException("Bank is required.");

            return new BankBranch
            {
                BankId = bankId,
                Name = Guard.NotBlank(name, "Branch name", 150),
                Address = Guard.NotBlank(address, "Branch address", 300),
                CreatedAt = now
            };
        }
    }
}
