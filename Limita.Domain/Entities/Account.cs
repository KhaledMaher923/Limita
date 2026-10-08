using Limita.Domain.Common;
using Limita.Domain.Enums;
using Limita.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace Limita.Domain.Entities
{
    public sealed class Account : Entity
    {
        public Account()
        {
        }

        public Guid UserId { get; private set; }
        public string AccountNumber { get; private set; } = string.Empty;
        public AccountType Type { get; private set; }
        public Money Balance { get; private set; } = null!;
        public AccountStatus Status { get; private set; }

        /// <summary>Optimistic concurrency token (SQL Server rowversion).</summary>
        public byte[] RowVersion { get; private set; } = [];

        public static Account Open(Guid userId, string accountNumber, AccountType type, string currency) => new()
        {
            UserId = userId,
            AccountNumber = accountNumber,
            Type = type,
            Balance = Money.Zero(currency),
            Status = AccountStatus.Active
        };

        public bool HasSufficientFunds(Money amount)
            => amount.Currency == Balance.Currency && Balance >= amount;

        /// <summary>
        /// The only way money enters an account. Returns the immutable ledger entry, which the caller must add to the database.
        /// </summary>
        public Transaction Credit(
            Money amount,
            TransactionCategory category,
            string? description,
            DateTimeOffset now,
            Guid? transferId = null,
            Guid? savingsGoalId = null)
        {
            EnsureCanTransact(amount);

            Balance += amount;

            return Transaction.Create(Id, TransactionType.Credit, amount, Balance.Amount, category, description, now, transferId, savingsGoalId);
        }

        /// <summary>
        /// The only way money leaves an account. Returns the immutable ledger entry, which the caller must add to the database.
        /// </summary>
        public Transaction Debit(
            Money amount,
            TransactionCategory category,
            string? description,
            DateTimeOffset now,
            Guid? transferId = null,
            Guid? savingsGoalId = null)
        {
            EnsureCanTransact(amount);

            if (Balance < amount)
                throw new DomainException("Insufficient funds.");

            Balance -= amount;

            return Transaction.Create(Id, TransactionType.Debit, amount, Balance.Amount, category, description, now, transferId, savingsGoalId);
        }

        public void Freeze()
        {
            if(Status == AccountStatus.Closed)
                throw new DomainException("A closed account cannot be frozen.");

            Status = AccountStatus.Frozen;
        }

        public void Unfreeze()
        {
            if(Status != AccountStatus.Frozen)
                throw new DomainException("Only a frozen account can be unfrozen.");

            Status = ~AccountStatus.Active;
        }

        public void Close()
        {
            if(!Balance.IsZero)
                throw new DomainException("An account must have a zero balance before it can be closed.");

            Status = AccountStatus.Closed;
        }

        internal void EnsureCanTransact(Money amount)
        {
            if(Status != AccountStatus.Active)
                throw new DomainException("The account is not active.");

            Guard.Positive(amount, "Amount");

            if (amount.Currency != Balance.Currency)
                throw new DomainException($"The account currency is {Balance.Currency}.");
        }
    }
}
