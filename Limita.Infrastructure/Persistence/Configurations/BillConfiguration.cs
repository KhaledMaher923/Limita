using Limita.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Limita.Infrastructure.Persistence.Configurations
{
    internal sealed class BillConfiguration : IEntityTypeConfiguration<Bill>
    {
        public void Configure(EntityTypeBuilder<Bill> builder)
        {
            builder.ToTable("Bills", t =>
            {
                t.HasCheckConstraint("CK_Bills_Fee_Positive", "[FeeAmount] > 0");
                t.HasCheckConstraint("CK_Bills_Tax_NonNegative", "[TaxAmount] >= 0");
            });
            builder.HasKey(b => b.Id);
            builder.Property(b => b.Id).ValueGeneratedNever();

            builder.Property(b => b.Type).HasConversion<string>().HasMaxLength(20);
            builder.Property(b => b.Code).HasMaxLength(50).IsRequired();
            builder.Property(b => b.CustomerName).HasMaxLength(100).IsRequired();
            builder.Property(b => b.Company).HasMaxLength(100);
            builder.Property(b => b.Status).HasConversion<string>().HasMaxLength(20);
            builder.Property(b => b.RowVersion).IsRowVersion();
            builder.HasMoney(b => b.Fee, "FeeAmount", "FeeCurrency");
            builder.HasMoney(b => b.Tax, "TaxAmount", "TaxCurrency");

            // A bill is looked up by its type and code, so the pair must be unique.
            builder.HasIndex(b => new { b.Type, b.Code }).IsUnique();

            // Smart Pay lists the unpaid bills of one customer, soonest due date first.
            builder.HasIndex(b => new { b.CustomerUserId, b.Status, b.DueDate });

            builder.HasOne<User>().WithMany().HasForeignKey(b => b.CustomerUserId).OnDelete(DeleteBehavior.Restrict);
        }
    }
}
