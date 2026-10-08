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
    /// <summary>
    /// A ledger entry. Immutable: there are no setters or mutating methods, and entries can only be created
    /// through Account.Credit / Account.Debit so a balance never changes without a matching entry.
    /// </summary>
    public sealed class Transaction : Entity
    {
        private Transaction() // required by EF Core
        {
        }

        public Guid AccountId { get; private set; }
        public Guid? TransferId { get; private set; }
        public Guid? SavingsGoalId { get; private set; }
        public Guid? WithdrawalId { get; private set; }
        public Guid? TimeDepositId { get; private set; }
        public TransactionType Type { get; private set; }
        public Money Amount { get; private set; } = null!;
        public decimal BalanceAfter { get; private set; }
        public TransactionCategory Category { get; private set; }
        public string? Description { get; private set; }
        public DateTimeOffset CreatedAt { get; private set; }

        internal static Transaction Create(
            Guid accountId,
            TransactionType type,
            Money amount,
            decimal balanceAfter,
            TransactionCategory category,
            string? description,
            DateTimeOffset now,
            Guid? transferId,
            Guid? savingsGoalId,
            Guid? withdrawalId,
            Guid? timeDepositId)
        {
            var transaction = new Transaction 
            {
                AccountId = accountId,
                TransferId = transferId,
                SavingsGoalId = savingsGoalId,
                WithdrawalId = withdrawalId,
                TimeDepositId = timeDepositId,
                Type = type,
                Amount = amount,
                BalanceAfter = balanceAfter,
                Category = category,
                Description = Guard.Optional(description, "Description", 200),
                CreatedAt = now
            };

            transaction.Raise(new TransactionCreatedEvent(transaction.Id, accountId, type, amount, category));

            return transaction;
        }


    }
}
