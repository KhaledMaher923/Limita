using Limita.Domain.Common;

namespace Limita.Domain.Entities
{
    public sealed class Bank : Entity
    {
        private Bank() // required by EF Core
        {
        }

        public string Name { get; private set; } = string.Empty;
        public string SwiftCode { get; private set; } = string.Empty;
        public DateTimeOffset CreatedAt { get; private set; }

        public static Bank Create(string name, string swiftCode, DateTimeOffset now) => new()
        {
            Name = Guard.NotBlank(name, "Bank name", 100),
            SwiftCode = Guard.NotBlank(swiftCode, "SWIFT code", 11),
            CreatedAt = now
        };
    }
}
