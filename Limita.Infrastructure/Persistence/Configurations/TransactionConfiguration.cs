using Limita.Domain.Entities;
using Limita.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Limita.Infrastructure.Persistence.Configurations;

internal sealed class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("Transactions");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.AccountId)
            .IsRequired();

        builder.Property(t => t.TransferId);

        builder.Property(t => t.SavingsGoalId);

        builder.Property(t => t.Type)
            .IsRequired();

        builder.OwnsOne(t => t.Amount, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("Amount")
                .HasPrecision(18, 2)
                .IsRequired();

            money.Property(m => m.Currency)
                .HasColumnName("Currency")
                .HasMaxLength(3)
                .IsRequired();
        });

        builder.Property(t => t.BalanceAfter)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(t => t.Category)
            .IsRequired();

        builder.Property(t => t.Description)
            .HasMaxLength(200);

        builder.Property(t => t.CreatedAt)
            .IsRequired();

        builder.HasIndex(t => t.AccountId);
        builder.HasIndex(t => t.TransferId);
        builder.HasIndex(t => t.CreatedAt);
    }
}