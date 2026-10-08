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
    public sealed class SavingsGoal : Entity
    {
        private SavingsGoal() // required by EF Core
        {
        }

        public Guid UserId { get; private set; }
        public Guid AccountId { get; private set; }
        public string Name { get; private set; } = string.Empty;
        public Money TargetAmount { get; private set; } = null!;
        public Money SavedAmount { get; private set; } = null!;
        public DateOnly? TargetDate { get; private set; }
        public SavingsGoalStatus Status { get; private set; }

        public decimal ProgressPercent => TargetAmount.Amount == 0m
            ? 0m
            : Math.Min(100m, Math.Round(SavedAmount.Amount / TargetAmount.Amount * 100m, 2));

        public static SavingsGoal Create(
            Guid userId,
            Guid accountId,
            string name,
            Money targetAmount,
            DateOnly? targetDate,
            DateOnly today)
        {
            Guard.Positive(targetAmount, "Target amount");

            if (targetDate is { } date && date < today)
                throw new DomainException("The target date cannot be in the past.");

            return new SavingsGoal
            {
                UserId = userId,
                AccountId = accountId,
                Name = Guard.NotBlank(name, "Goal name", 100),
                TargetAmount = targetAmount,
                SavedAmount = Money.Zero(targetAmount.Currency),
                TargetDate = targetDate,
                Status = SavingsGoalStatus.Active
            };
        }

        /// <summary>
        /// Moves money from the funding account into the goal. The contribution goes through the ledger:
        /// the returned entry must be added to the database by the caller.
        /// </summary>
        public Transaction Contribute(Account account, Money amount, DateTimeOffset now)
        {
            if (Status != SavingsGoalStatus.Active)
                throw new DomainException("Only an active goal can receive contributions.");

            if (account.Id != AccountId)
                throw new DomainException("Contributions must come from the goal's funding account.");

            if (amount.Currency != TargetAmount.Currency)
                throw new DomainException($"The goal currency is {TargetAmount.Currency}.");

            var entry = account.Debit(amount, TransactionCategory.Savings, $"Savings goal: {Name}", now, savingsGoalId: Id);

            SavedAmount += amount;

            if(SavedAmount >= TargetAmount)
            {
                Status = SavingsGoalStatus.Completed;
                Raise(new SavingsGoalReachedEvent(Id, UserId, Name));
            }

            return entry;
        }

        public void Cancel()
        {
            if (Status == SavingsGoalStatus.Completed)
                throw new DomainException("A completed goal cannot be cancelled.");

            Status = SavingsGoalStatus.Cancelled;
        }
    }
}
