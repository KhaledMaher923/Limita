using Limita.Domain.Common;
using Limita.Domain.Enums;
using Limita.Domain.Events;
using Limita.Domain.ValueObjects;
using System;
using System.Linq;

namespace Limita.Domain.Entities
{
    /// <summary>A prepaid mobile recharge paid from an account.</summary>
    public sealed class MobileTopUp : Entity
    {
        private MobileTopUp() // required by EF Core
        {
        }

        public Guid UserId { get; private set; }
        public Guid AccountId { get; private set; }

        /// <summary>Id of the ledger entry created by the debit.</summary>
        public Guid TransactionId { get; private set; }

        public string PhoneNumber { get; private set; } = string.Empty;
        public Money Amount { get; private set; } = null!;

        /// <summary>Unique per request, so a retried request cannot recharge twice.</summary>
        public string IdempotencyKey { get; private set; } = string.Empty;

        public DateTimeOffset CreatedAt { get; private set; }

        /// <summary>
        /// Recharges a phone from an account. The caller adds the top-up and the ledger entry to the database
        /// and saves once, inside the command's transaction.
        /// </summary>
        public static MobileTopUpResult Execute(
            Account account,
            Guid userId,
            string phoneNumber,
            Money amount,
            string idempotencyKey,
            DateTimeOffset now)
        {
            Guard.Positive(amount, "Top-up amount");

            var phone = NormalizePhone(phoneNumber);

            // Validate everything before touching the balance.
            account.EnsureCanTransact(amount);

            if (!account.HasSufficientFunds(amount))
                throw new DomainException("Insufficient funds.");

            var topUp = new MobileTopUp
            {
                UserId = userId,
                AccountId = account.Id,
                PhoneNumber = phone,
                Amount = amount,
                IdempotencyKey = Guard.NotBlank(idempotencyKey, "Idempotency key", 100),
                CreatedAt = now
            };

            var debit = account.Debit(
                amount,
                TransactionCategory.MobileTopUp,
                "Mobile top-up",
                now);

            topUp.TransactionId = debit.Id;

            topUp.Raise(new MobileTopUpCompletedEvent(topUp.Id, userId, amount));

            return new MobileTopUpResult(topUp, debit);
        }

        /// <summary>Accepts an optional leading + followed by 8 to 15 digits.</summary>
        private static string NormalizePhone(string? phoneNumber)
        {
            var phone = Guard.NotBlank(phoneNumber, "Phone number", 20);

            var digits = phone.StartsWith('+') ? phone[1..] : phone;

            if (digits.Length is < 8 or > 15 || !digits.All(char.IsAsciiDigit))
                throw new DomainException("The phone number must contain 8 to 15 digits.");

            return phone;
        }
    }

    public sealed record MobileTopUpResult(MobileTopUp TopUp, Transaction Debit);
}
