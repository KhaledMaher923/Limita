using Limita.Domain.Common;
using Limita.Domain.Enums;

namespace Limita.Domain.Entities
{
    public sealed class Beneficiary : Entity
    {
        private Beneficiary() // required by EF Core
        {
        }

        public Guid UserId { get; private set; }
        public string Name { get; private set; } = string.Empty;
        public TransferType TransferType { get; private set; }

        /// <summary>Used when TransferType is CardNumber. Only the last 4 digits are stored.</summary>
        public string? CardNumberLast4 { get; private set; }

        /// <summary>A hashed representation of the full card number, for processing only.</summary>
        public string? CardNumberHash { get; private set; }

        /// <summary>Used when TransferType is SameBank or OtherBank.</summary>
        public string? AccountNumber { get; private set; }

        public Guid? BankId { get; private set; }
        public Guid? BranchId { get; private set; }
        public DateTimeOffset CreatedAt { get; private set; }

        public static Beneficiary CreateForCard(
            Guid userId,
            string name,
            string cardNumberLast4,
            string cardNumberHash,
            DateTimeOffset now)
        {
            if (userId == Guid.Empty)
                throw new DomainException("User is required.");

            if (cardNumberLast4 is not { Length: 4 } || !cardNumberLast4.All(char.IsAsciiDigit))
                throw new DomainException("The last four digits must be exactly four digits.");

            return new Beneficiary
            {
                UserId = userId,
                Name = Guard.NotBlank(name, "Beneficiary name", 100),
                TransferType = TransferType.CardNumber,
                CardNumberLast4 = cardNumberLast4,
                CardNumberHash = Guard.NotBlank(cardNumberHash, "Card number hash", 256),
                CreatedAt = now
            };
        }

        public static Beneficiary CreateForBank(
            Guid userId,
            string name,
            TransferType transferType,
            string accountNumber,
            Guid bankId,
            Guid branchId,
            DateTimeOffset now)
        {
            if (userId == Guid.Empty)
                throw new DomainException("User is required.");

            if (transferType == TransferType.CardNumber)
                throw new DomainException("Use CreateForCard for card-number beneficiaries.");

            if (bankId == Guid.Empty)
                throw new DomainException("Bank is required for bank transfers.");

            if (branchId == Guid.Empty)
                throw new DomainException("Branch is required for bank transfers.");

            return new Beneficiary
            {
                UserId = userId,
                Name = Guard.NotBlank(name, "Beneficiary name", 100),
                TransferType = transferType,
                AccountNumber = Guard.NotBlank(accountNumber, "Account number", 30),
                BankId = bankId,
                BranchId = branchId,
                CreatedAt = now
            };
        }

        /// <summary>Returns the masked card number for display (e.g. **** **** **** 1234).</summary>
        public string? MaskedCardNumber => CardNumberLast4 is not null
            ? $"**** **** **** {CardNumberLast4}"
            : null;
    }
}
