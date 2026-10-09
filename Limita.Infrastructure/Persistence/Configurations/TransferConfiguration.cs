using Limita.Domain.Entities;
using Limita.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Limita.Infrastructure.Persistence.Configurations
{
    internal sealed class TransferConfiguration : IEntityTypeConfiguration<Transfer>
    {
        public void Configure(EntityTypeBuilder<Transfer> builder)
        {
            builder.ToTable("Transfers", t => t.HasCheckConstraint("CK_Transfers_Amount_Positive", "[Amount] > 0"));
            builder.HasKey(t => t.Id);
            builder.Property(t => t.Id).ValueGeneratedNever();

            builder.Property(t => t.Status).HasConversion<string>().HasMaxLength(20);
            builder.Property(t => t.IdempotencyKey).HasMaxLength(100);
            builder.Property(t => t.Note).HasMaxLength(200);
            builder.HasMoney(t => t.Amount, "Amount", "Currency");

            // Member 3 fields
            builder.Property(t => t.TransferType).HasConversion<string>().HasMaxLength(20);
            builder.Property(t => t.VerificationMethod).HasConversion<string>().HasMaxLength(20);
            builder.Property(t => t.FailureReason).HasMaxLength(500);

            // Fee is nullable Money
            builder.ComplexProperty(t => t.Fee!, fee =>
            {
                fee.Property(m => m.Amount).HasColumnName("FeeAmount").HasPrecision(18, 2);
                fee.Property(m => m.Currency).HasColumnName("FeeCurrency").HasMaxLength(3).IsFixedLength().IsUnicode(false);
                fee.IsRequired(false);
            });

            builder.HasIndex(t => t.IdempotencyKey).IsUnique().HasFilter("[IdempotencyKey] IS NOT NULL AND [IdempotencyKey] <> ''");
            builder.HasIndex(t => t.FromAccountId);
            builder.HasIndex(t => t.ToAccountId);
            builder.HasIndex(t => t.CustomerId);

            builder.HasOne<Account>().WithMany().HasForeignKey(t => t.FromAccountId).OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<Beneficiary>()
                .WithMany()
                .HasForeignKey(t => t.BeneficiaryId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
