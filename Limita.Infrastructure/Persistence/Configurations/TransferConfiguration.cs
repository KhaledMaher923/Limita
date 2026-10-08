using Limita.Domain.Entities;
using Limita.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Limita.Infrastructure.Persistence.Configurations;

internal sealed class TransferConfiguration : IEntityTypeConfiguration<Transfer>
{
    public void Configure(EntityTypeBuilder<Transfer> builder)
    {
        builder.ToTable("Transfers");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.FromAccountId)
            .IsRequired();

        builder.Property(t => t.ToAccountId)
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

        builder.Property(t => t.Status)
            .IsRequired();

        builder.Property(t => t.IdempotencyKey)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(t => t.Note)
            .HasMaxLength(200);

        builder.Property(t => t.CreatedAt)
            .IsRequired();

        builder.HasIndex(t => t.FromAccountId);
        builder.HasIndex(t => t.ToAccountId);
        builder.HasIndex(t => t.IdempotencyKey).IsUnique();
        builder.HasIndex(t => t.CreatedAt);
    }
}