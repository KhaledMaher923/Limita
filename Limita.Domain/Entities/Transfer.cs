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
    }
    public sealed record TransferResult(Transfer Transfer, Transaction Debit, Transaction Credit);
}
