using Limita.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Limita.Infrastructure.Persistence.Configurations
{
    internal static class MoneyMapping
    {
        /// <summary>Maps a Money property to two columns: decimal(18,2) amount and a fixed three-letter currency.</summary>
        public static EntityTypeBuilder<T> HasMoney<T>(
            this EntityTypeBuilder<T> builder,
            Expression<Func<T,Money>> property,
            string amountColumn,
            string currencyColoumn) where T : class
        {
            builder.ComplexProperty(property, money =>
            {
                money.Property(m => m.Amount)
                .HasColumnName(amountColumn)
                .HasPrecision(18, 2);

                money.Property(m => m.Currency)
                .HasColumnName(currencyColoumn)
                .HasMaxLength(3)
                .IsFixedLength()
                .IsUnicode(false);
            });

            return builder;
        }
    }
}
