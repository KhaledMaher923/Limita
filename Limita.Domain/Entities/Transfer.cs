using Limita.Domain.Common;
using Limita.Domain.Enums;
using Limita.Domain.Events;
using Limita.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Limita.Domain.Entities
{
    public sealed class Transfer : Entity
    {
        private Transfer() // required by EF Core
        {
        }

        public Guid FromAccountId { get; private set; }
        public Guid ToAccountId { get; private set; }
        public Money Amount { get; private set; } = null!;
        public TransferStatus Status { get; private set; }

        /// <summary>Unique per transfer request, so a retried request cannot move money twice.</summary>
        public string IdempotencyKey { get; private set; } = string.Empty;

        public string? Note { get; private set; }
        public DateTimeOffset CreatedAt { get; private set; }

        // ── Member 3 fields ──────────────────────────────────────────
        public Guid? CustomerId { get; private set; }
        public TransferType TransferType { get; private set; }
        public VerificationMethod VerificationMethod { get; private set; }
        public Guid? BeneficiaryId { get; private set; }
        public Money? Fee { get; private set; }
        public string? FailureReason { get; private set; }

        /// <summary>
        /// Moves money between two accounts. The caller adds the transfer and both ledger entries to the database
        /// and saves once, inside the command's transaction.
        /// </summary>
        public static TransferResult Execute(
            Account from,
            Account to,
            Money amount,
            string idempotencyKey,
            string? note,
            DateTimeOffset now)
        {
            if (from.Id == to.Id)
                throw new DomainException("You cannot transfer to the same account.");

            Guard.Positive(amount, "Transfer amount");

            // Validate both sides before touching either balance.
            from.EnsureCanTransact(amount);
            to.EnsureCanTransact(amount);

            var transfer = new Transfer
            {
                FromAccountId = from.Id,
                ToAccountId = to.Id,
                Amount = amount,
                Status = TransferStatus.Completed,
                IdempotencyKey = Guard.NotBlank(idempotencyKey, "Idempotency key", 100),
                Note = Guard.Optional(note, "Note", 200),
                CreatedAt = now
            };

            var debit = from.Debit(amount, TransactionCategory.Transfer, transfer.Note, now, transferId: transfer.Id);
            var credit = to.Credit(amount, TransactionCategory.Transfer, transfer.Note, now, transferId: transfer.Id);

            transfer.Raise(new TransferCompletedEvent(transfer.Id, from.Id, to.Id, amount));

            return new TransferResult(transfer, debit, credit);
        }

        /// <summary>
        /// Creates a transfer record for Member 3 flows (bank/card transfers).
        /// The transfer is NOT financially completed — it stays Pending until ledger integration is available.
        /// </summary>
        public static Transfer CreatePending(
            Guid customerId,
            Guid fromAccountId,
            TransferType transferType,
            VerificationMethod verificationMethod,
            Money amount,
            Money fee,
            Guid? beneficiaryId,
            string? note,
            DateTimeOffset now)
        {
            Guard.Positive(amount, "Transfer amount");

            return new Transfer
            {
                CustomerId = customerId,
                FromAccountId = fromAccountId,
                ToAccountId = Guid.Empty, // external transfer, no internal target
                TransferType = transferType,
                VerificationMethod = verificationMethod,
                Amount = amount,
                Fee = fee,
                BeneficiaryId = beneficiaryId,
                Status = TransferStatus.Pending,
                Note = Guard.Optional(note, "Note", 200),
                CreatedAt = now
            };
        }

        public void MarkFailed(string reason)
        {
            if (Status != TransferStatus.Pending)
                throw new DomainException("Only a pending transfer can be marked as failed.");

            Status = TransferStatus.Failed;
            FailureReason = reason;
        }
    }
    public sealed record TransferResult(Transfer Transfer, Transaction Debit, Transaction Credit);
}
