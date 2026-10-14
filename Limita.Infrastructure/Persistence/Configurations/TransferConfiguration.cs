using Limita.Domain.Entities;
using Limita.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Limita.Infrastructure.Persistence.Configurations
{
    internal sealed class TransferConfiguration : IEntityTypeConfiguration<Transfer>
    {
        public void Configure(EntityTypeBuilder<Transfer> builder)
        {
            builder.ToTable("Transfers", t =>
            {
                t.HasCheckConstraint("CK_Transfers_Amount_Positive", "[Amount] > 0");
                t.HasCheckConstraint("CK_Transfers_Fee_NonNegative", "[FeeAmount] >= 0");
            });
            builder.HasKey(t => t.Id);
            builder.Property(t => t.Id).ValueGeneratedNever();

            builder.Property(t => t.Status).HasConversion<string>().HasMaxLength(20);
            builder.Property(t => t.TransferType).HasConversion<string>().HasMaxLength(20);
            builder.Property(t => t.VerificationMethod).HasConversion<string>().HasMaxLength(20);
            builder.Property(t => t.FailureReason).HasMaxLength(500);

            // EF Core 8 does not support optional complex types, so the fee is always present (zero when there is none).
            builder.HasMoney(t => t.Fee, "FeeAmount", "FeeCurrency");

            //// Fee is nullable Money
            //builder.ComplexProperty(t => t.Fee!, fee =>
            //{
            //    fee.Property(m => m.Amount).HasColumnName("FeeAmount").HasPrecision(18, 2);
            //    fee.Property(m => m.Currency).HasColumnName("FeeCurrency").HasMaxLength(3).IsFixedLength().IsUnicode(false);
            //    fee.IsRequired(false);
            //});

            builder.HasIndex(t => t.CustomerId);
            builder.HasOne<Beneficiary>()
                .WithMany()
                .HasForeignKey(t => t.BeneficiaryId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.Property(t => t.IdempotencyKey).HasMaxLength(100).IsRequired();
            builder.Property(t => t.Note).HasMaxLength(200);
            builder.HasMoney(t => t.Amount, "Amount", "Currency");

            // Unique per sending account (not globally), so one customer's key can never collide with another's.
            builder.HasIndex(t => new { t.FromAccountId, t.IdempotencyKey }).IsUnique();
            builder.HasIndex(t => t.FromAccountId);
            builder.HasIndex(t => t.ToAccountId);

            builder.HasOne<Account>().WithMany().HasForeignKey(t => t.FromAccountId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<Account>().WithMany().HasForeignKey(t => t.ToAccountId).OnDelete(DeleteBehavior.Restrict);
        }
    }
}
