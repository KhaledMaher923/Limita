using Limita.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Limita.Infrastructure.Persistence.Configurations;

internal sealed class ExchangeOperationConfiguration
    : IEntityTypeConfiguration<ExchangeOperation>
{
    public void Configure(EntityTypeBuilder<ExchangeOperation> builder)
    {
        builder.ToTable("ExchangeOperations");

        builder.HasKey(operation => operation.Id);
        builder.Property(operation => operation.Id).ValueGeneratedNever();

        builder.Property(operation => operation.ExchangeRate)
            .HasPrecision(18, 6)
            .IsRequired();

        builder.Property(operation => operation.CreatedAt)
            .IsRequired();

        builder.HasMoney(
            operation => operation.SourceAmount,
            "SourceAmount",
            "SourceCurrency");

        builder.HasMoney(
            operation => operation.TargetAmount,
            "TargetAmount",
            "TargetCurrency");

        builder.HasIndex(operation => new
        {
            operation.UserId,
            operation.CreatedAt
        });

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(operation => operation.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Account>()
            .WithMany()
            .HasForeignKey(operation => operation.SourceAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Account>()
            .WithMany()
            .HasForeignKey(operation => operation.TargetAccountId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}