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
    public sealed class SpendingLimit : Entity
    {
        private SpendingLimit() // required by EF Core
        {
        }

        public Guid UserId { get; private set; }
        public Guid? CardId { get; private set; }
        public Guid? AccountId { get; private set; }
        public TransactionCategory? Category { get; private set; }
        public LimitPeriod Period { get; private set; }
        public Money LimitAmount { get; private set; } = null!;
        public bool IsActive { get; private set; }

        public static SpendingLimit Create(
            Guid userId,
            Guid? cardId,
            Guid? accountId,
            TransactionCategory? category,
            LimitPeriod period,
            Money limitAmount)
        {
            if (cardId is null && accountId is null)
                throw new DomainException("A spending limit must target a card or an account.");

            Guard.Positive(limitAmount, "Limit amount");

            return new SpendingLimit
            {
                UserId = userId,
                CardId = cardId,
                AccountId = accountId,
                Category = category,
                Period = period,
                LimitAmount = limitAmount,
                IsActive = true
            };
        }

        public void ChangeLimit(Money newLimit)
        {
            Guard.Positive(newLimit, "Limit amount");

            if (newLimit.Currency != LimitAmount.Currency)
                throw new DomainException("The limit currency cannot be changed.");

            LimitAmount = newLimit;
        }

        public void Activate() => IsActive = true;

        public void Deactivate() => IsActive = false;

        /// <summary>Start of the current limit window in UTC (weeks start on Monday).</summary>
        public DateTimeOffset GetPeriodStart(DateTimeOffset now)
        {
            var utc = now.ToUniversalTime();
            var today = new DateTimeOffset(utc.Year, utc.Month, utc.Day, 0, 0, 0, TimeSpan.Zero);

            return Period switch
            {
                LimitPeriod.Daily => today,
                LimitPeriod.Weekly => today.AddDays(-(((int)today.DayOfWeek + 6) % 7)),
                LimitPeriod.Monthly => new DateTimeOffset(utc.Year, utc.Month, 1, 0, 0, 0, TimeSpan.Zero),
                _ => throw new DomainException("Unknown limit period.")
            };
        }
        
        /// <summary>True when spending is strictly above the limit. Spending exactly at the limit is allowed.</summary>
        public bool IsExceededBy(Money spentSoFar) => spentSoFar > LimitAmount;

        public void ReportExceeded(Money spentSoFar)
            => Raise(new SpendingLimitExceededEvent(Id, UserId, LimitAmount, spentSoFar));
    }
}
