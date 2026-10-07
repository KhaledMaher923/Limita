using Limita.Domain.Common;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Limita.Domain.ValueObjects
{
    /// <summary>
    /// An amount of money in a specific currency. Money is always a decimal, never a double or float.
    /// Rule for the MVP: at most two decimal places (matches the decimal(18,2) database column).
    /// </summary>
    public sealed record Money
    {
        public decimal Amount { get; }
        public string Currency { get; }

        public Money(decimal amount, string currency)
        {
            Amount = amount;
            Currency = currency;
        }

        public static Money Of(decimal amount, string currency)
        {
            if (string.IsNullOrWhiteSpace(currency) || currency.Length != 3 || !currency.All(char.IsAsciiLetter))
                throw new DomainException("Currency must be a three-letter ISO 4217 code.");

            if (decimal.Round(amount, 2) != amount)
                throw new DomainException("Money cannot have more than two decimal places.");

            return new Money(amount, currency.ToUpperInvariant());
        }

        public static Money Zero(string currency) => Of(0m, currency);

        public bool IsZero => Amount == 0m;
        public bool IsNegative => Amount < 0m;

        public Money Add(Money other)
        {
            EnsureSameCurrency(other);
            return new Money(Amount + other.Amount, Currency);
        }

        public Money Subtract(Money other)
        {
            EnsureSameCurrency(other);
            return new Money(Amount - other.Amount, Currency);
        }

        public int CompareTo(Money other)
        {
            EnsureSameCurrency(other);
            return Amount.CompareTo(other.Amount);
        }

        public static Money operator +(Money a, Money b) => a.Add(b);
        public static Money operator -(Money a, Money b) => a.Subtract(b);
        public static bool operator >(Money a, Money b) => a.CompareTo(b) > 0;
        public static bool operator <(Money a, Money b) => a.CompareTo(b) < 0;
        public static bool operator >=(Money a, Money b) => a.CompareTo(b) >= 0;
        public static bool operator <=(Money a, Money b) => a.CompareTo(b) <= 0;

        public override string ToString() => $"{Amount.ToString("0.00", CultureInfo.InvariantCulture)} {Currency}";

        private void EnsureSameCurrency(Money other)
        {
            if (Currency != other.Currency)
                throw new DomainException($"Currency mismatch: {Currency} and {other.Currency}.");
        }
    }
}
