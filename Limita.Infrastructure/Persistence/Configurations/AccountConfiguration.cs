using Limita.Domain.Entities;
using Limita.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Limita.Infrastructure.Persistence.Configurations;

internal sealed class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("Accounts");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.UserId)
            .IsRequired();

        builder.Property(a => a.AccountNumber)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(a => a.Type)
            .IsRequired();

        builder.Property(a => a.Status)
            .IsRequired();

        builder.OwnsOne(a => a.Balance, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("Balance")
                .HasPrecision(18, 2)
                .IsRequired();

            money.Property(m => m.Currency)
                .HasColumnName("Currency")
                .HasMaxLength(3)
                .IsRequired();
        });

        builder.Property(a => a.RowVersion)
            .IsRowVersion()
            .IsConcurrencyToken();

        builder.HasIndex(a => a.UserId);
        builder.HasIndex(a => a.AccountNumber).IsUnique();
    }
}