using Limita.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Limita.Infrastructure.Persistence.Configurations
{
    internal sealed class BillPaymentConfiguration : IEntityTypeConfiguration<BillPayment>
    {
        public void Configure(EntityTypeBuilder<BillPayment> builder)
        {
            builder.ToTable("BillPayments", t => t.HasCheckConstraint("CK_BillPayments_Amount_Positive", "[Amount] > 0"));
            builder.HasKey(p => p.Id);
            builder.Property(p => p.Id).ValueGeneratedNever();

            builder.Property(p => p.BillType).HasConversion<string>().HasMaxLength(20);
            builder.Property(p => p.IdempotencyKey).HasMaxLength(100).IsRequired();
            builder.HasMoney(p => p.Amount, "Amount", "Currency");

            // The last line of defence against paying twice when two identical requests arrive together.
            builder.HasIndex(p => new { p.UserId, p.IdempotencyKey }).IsUnique();

            // Payment history: one customer, newest first.
            builder.HasIndex(p => new { p.UserId, p.CreatedAt });
            builder.HasIndex(p => p.BillId);

            builder.HasOne<User>().WithMany().HasForeignKey(p => p.UserId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<Bill>().WithMany().HasForeignKey(p => p.BillId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<Account>().WithMany().HasForeignKey(p => p.AccountId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<Transaction>().WithMany().HasForeignKey(p => p.TransactionId).OnDelete(DeleteBehavior.Restrict);
        }
    }
}
