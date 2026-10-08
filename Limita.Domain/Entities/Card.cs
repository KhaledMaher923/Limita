using Limita.Domain.Common;
using Limita.Domain.Enums;
using Limita.Domain.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Limita.Domain.Entities
{
    /// <summary>
    /// A payment card. Only the last four digits and a payment-provider token are stored.
    /// The full card number and CVV must never reach this system's database.
    /// </summary>
    public sealed class Card : Entity
    {
        private Card() // required by EF Core
        {
        }

        public Guid AccountId { get; private set; }
        public CardNetwork Network { get; private set; }
        public string Last4 { get; private set; } = string.Empty;
        public int ExpiryMonth { get; private set; }
        public int ExpiryYear { get; private set; }
        public string HolderName { get; private set; } = string.Empty;
        public CardStatus Status { get; private set; }
        public string Token { get; private set; } = string.Empty;

        public static Card Add(
            Guid accountId,
            CardNetwork network,
            string last4,
            int expiryMonth,
            int expiryYear,
            string holderName,
            string token,
            DateTimeOffset now)
        {
            if(last4 is not { Length: 4 } || !last4.All(char.IsAsciiDigit))
                throw new DomainException("The last four digits must be exactly four digits.");

            if(expiryMonth is < 1 or > 12)
                throw new DomainException("The expiry month must be between 1 and 12.");

            if (expiryYear is < 2000 or > 2100)
                throw new DomainException("The expiry year must be a four-digit year.");

            var card = new Card 
            {
                AccountId = accountId,
                Network = network,
                Last4 = last4,
                ExpiryMonth = expiryMonth,
                ExpiryYear = expiryYear,
                HolderName = Guard.NotBlank(holderName, "Card holder name", 100),
                Token = Guard.NotBlank(token, "Card token", 200),
                Status = CardStatus.Active
            };

            if (card.IsExpired(now))
                throw new DomainException("The card is expired.");

            card.Raise(new CardAddedEvent(card.Id, accountId));

            return card;
        }

        public bool IsExpired(DateTimeOffset now)
            => ExpiryYear < now.Year || (ExpiryYear == now.Year && ExpiryMonth < now.Month);

        public void Block()
        {
            if (Status == CardStatus.Closed)
                throw new DomainException("A closed card cannot be blocked.");

            Status = CardStatus.Blocked;
        }
        public void Activate(DateTimeOffset now)
        {
            if (Status == CardStatus.Closed)
                throw new DomainException("A closed card cannot be activated.");

            if (IsExpired(now))
                throw new DomainException("An expired card cannot be activated.");

            Status = CardStatus.Active;
        }

        public void Close() => Status = CardStatus.Closed;

    }
}
