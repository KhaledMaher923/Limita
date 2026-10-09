using Limita.Domain.Common;
using Limita.Domain.Enums;
using Limita.Domain.ValueObjects;
using System;

namespace Limita.Domain.Entities
{
    /// <summary>
    /// A utility bill that a customer looks up by its code and pays (electric, water, internet, mobile).
    /// A bill is not owned by a user: anyone who knows the code can look it up. Who paid it is recorded in BillPayment.
    /// </summary>
    public sealed class Bill : Entity
    {
        private Bill() // required by EF Core
        {
        }

        public BillType Type { get; private set; }
        public string Code { get; private set; } = string.Empty;
        public string CustomerName { get; private set; } = string.Empty;
        public string? Company { get; private set; }
        public DateOnly PeriodFrom { get; private set; }
        public DateOnly PeriodTo { get; private set; }
        public Money Fee { get; private set; } = null!;
        public Money Tax { get; private set; } = null!;
        public DateOnly DueDate { get; private set; }
        public BillStatus Status { get; private set; }

        /// <summary>The customer this bill is linked to. Only Smart Pay uses it; looking a bill up by code does not need it.</summary>
        public Guid? CustomerUserId { get; private set; }

        /// <summary>Optimistic concurrency token (SQL Server rowversion). Stops two payments from settling the same bill.</summary>
        public byte[] RowVersion { get; private set; } = [];

        /// <summary>The amount the customer pays: fee plus tax.</summary>
        public Money Total => Fee + Tax;

        public static Bill Create(
            BillType type,
            string code,
            string customerName,
            string? company,
            DateOnly periodFrom,
            DateOnly periodTo,
            Money fee,
            Money tax,
            DateOnly dueDate,
            Guid? customerUserId = null)
        {
            if (periodTo < periodFrom)
                throw new DomainException("The billing period end cannot be before its start.");

            Guard.Positive(fee, "Fee");

            if (tax.Amount < 0m)
                throw new DomainException("Tax cannot be negative.");

            if (tax.Currency != fee.Currency)
                throw new DomainException("The fee and the tax must use the same currency.");

            return new Bill
            {
                Type = type,
                Code = Guard.NotBlank(code, "Bill code", 50),
                CustomerName = Guard.NotBlank(customerName, "Customer name", 100),
                Company = Guard.Optional(company, "Company", 100),
                PeriodFrom = periodFrom,
                PeriodTo = periodTo,
                Fee = fee,
                Tax = tax,
                DueDate = dueDate,
                CustomerUserId = customerUserId,
                Status = BillStatus.Unpaid
            };
        }

        public bool IsPaid => Status == BillStatus.Paid;

        /// <summary>True when the bill is unpaid and its due date falls on or before the given day.</summary>
        public bool IsDueBy(DateOnly day) => Status == BillStatus.Unpaid && DueDate <= day;

        /// <summary>Marks the bill as settled. Called only from BillPayment.Execute so a bill cannot be paid without a payment record.</summary>
        internal void MarkAsPaid()
        {
            if (Status == BillStatus.Paid)
                throw new DomainException("This bill is already paid.");

            Status = BillStatus.Paid;
        }
    }
}
