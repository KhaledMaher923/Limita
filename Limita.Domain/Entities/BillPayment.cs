using Limita.Domain.Common;
using Limita.Domain.Enums;
using Limita.Domain.Events;
using Limita.Domain.ValueObjects;
using System;

namespace Limita.Domain.Entities
{
    /// <summary>A completed payment of one bill. It is the record shown in Payment history.</summary>
    public sealed class BillPayment : Entity
    {
        private BillPayment() // required by EF Core
        {
        }

        public Guid UserId { get; private set; }
        public Guid BillId { get; private set; }
        public Guid AccountId { get; private set; }

        /// <summary>Id of the ledger entry created by the debit, so the payment can be traced to the account statement.</summary>
        public Guid TransactionId { get; private set; }

        /// <summary>Copied from the bill so Payment history can be filtered by type without a join.</summary>
        public BillType BillType { get; private set; }

        public Money Amount { get; private set; } = null!;

        /// <summary>Unique per payment request, so a retried request cannot pay twice.</summary>
        public string IdempotencyKey { get; private set; } = string.Empty;

        public DateTimeOffset CreatedAt { get; private set; }

        /// <summary>
        /// Pays a bill from an account. The caller adds the payment and the ledger entry to the database
        /// and saves once, inside the command's transaction.
        /// </summary>
        public static BillPaymentResult Execute(
            Account account,
            Bill bill,
            Guid userId,
            string idempotencyKey,
            DateTimeOffset now)
        {
            var amount = bill.Total;

            Guard.Positive(amount, "Bill amount");

            // Validate everything before touching the bill or the balance.
            account.EnsureCanTransact(amount);

            if (!account.HasSufficientFunds(amount))
                throw new DomainException("Insufficient funds.");

            bill.MarkAsPaid();

            var payment = new BillPayment
            {
                UserId = userId,
                BillId = bill.Id,
                AccountId = account.Id,
                BillType = bill.Type,
                Amount = amount,
                IdempotencyKey = Guard.NotBlank(idempotencyKey, "Idempotency key", 100),
                CreatedAt = now
            };

            var debit = account.Debit(
                amount,
                TransactionCategory.Bills,
                $"{bill.Type} bill {bill.Code}",
                now);

            payment.TransactionId = debit.Id;

            payment.Raise(new BillPaidEvent(payment.Id, userId, bill.Type, amount));

            return new BillPaymentResult(payment, debit);
        }
    }

    public sealed record BillPaymentResult(BillPayment Payment, Transaction Debit);
}
