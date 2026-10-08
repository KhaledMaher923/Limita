using Limita.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Limita.Domain.Common
{
    internal static class Guard
    {
        public static string NotBlank(string? value, string field, int maxLength)
        {
            if(string.IsNullOrWhiteSpace(value)) 
                throw new DomainException($"{field} is required.");

            var trimmed = value.Trim();
            if(trimmed.Length > maxLength)
                throw new DomainException($"{field} cannot exceed {maxLength} characters.");

            return trimmed;
        }

        public static string? Optional(string? value, string field, int maxLength)
        {
            if(string.IsNullOrWhiteSpace(value))
                return null;

            var trimmed = value.Trim();
            if (trimmed.Length > maxLength)
                throw new DomainException($"{field} cannot exceed {maxLength} characters.");

            return trimmed;
        }

        public static Money Positive(Money amount, string field)
        {
            if (amount.Amount <= 0m)
                throw new DomainException($"{field} must be greater than zero.");

            return amount;
        }
    }
}
