using Limita.Domain.Entities;
using Limita.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Limita.Infrastructure.Persistence.Configurations;

internal sealed class SpendingLimitConfiguration : IEntityTypeConfiguration<SpendingLimit>
{
    public void Configure(EntityTypeBuilder<SpendingLimit> builder)
    {
        builder.ToTable("SpendingLimits");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.UserId)
            .IsRequired();

        builder.Property(s => s.CardId);

        builder.Property(s => s.AccountId);

        builder.Property(s => s.Category);

        builder.Property(s => s.Period)
            .IsRequired();

        builder.OwnsOne(s => s.LimitAmount, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("LimitAmount")
                .HasPrecision(18, 2)
                .IsRequired();

            money.Property(m => m.Currency)
                .HasColumnName("LimitCurrency")
                .HasMaxLength(3)
                .IsRequired();
        });

        builder.Property(s => s.IsActive)
            .IsRequired();

        builder.HasIndex(s => s.UserId);
    }
}