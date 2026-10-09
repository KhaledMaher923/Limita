using Limita.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Limita.Infrastructure.Persistence.Configurations
{
    internal sealed class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
    {
        public void Configure(EntityTypeBuilder<Transaction> builder)
        {
            builder.ToTable("Transactions");
            builder.HasKey(t => t.Id);
            builder.Property(t => t.Id).ValueGeneratedNever();

            builder.Property(t => t.Type).HasConversion<string>().HasMaxLength(10);
            builder.Property(t => t.Category).HasConversion<string>().HasMaxLength(30);
            builder.Property(t => t.Description).HasMaxLength(200);
            builder.HasMoney(t => t.Amount, "Amount", "Currency");

            builder.HasIndex(t => t.AccountId);
            builder.HasIndex(t => t.TransferId);

            builder.HasOne<Account>()
                .WithMany()
                .HasForeignKey(t => t.AccountId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
