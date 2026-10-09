using Limita.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Limita.Infrastructure.Persistence.Configurations;

public sealed class InterestRateConfiguration : IEntityTypeConfiguration<InterestRate>
{
    public void Configure(EntityTypeBuilder<InterestRate> builder)
    {
        builder.ToTable("InterestRates");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ProductCode).HasMaxLength(30).IsRequired();
        builder.Property(x => x.ProductName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.CurrencyCode).HasMaxLength(3).IsRequired();
        builder.Property(x => x.AnnualRatePercent).HasPrecision(9, 4);
        builder.Property(x => x.MinimumAmount).HasPrecision(18, 2);
        builder.HasIndex(x => new { x.ProductCode, x.EffectiveFrom });
    }
}
