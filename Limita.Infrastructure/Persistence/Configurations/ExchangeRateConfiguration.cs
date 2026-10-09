using Limita.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Limita.Infrastructure.Persistence.Configurations;

public sealed class ExchangeRateConfiguration : IEntityTypeConfiguration<ExchangeRate>
{
    public void Configure(EntityTypeBuilder<ExchangeRate> builder)
    {
        builder.ToTable("ExchangeRates");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.BaseCurrency).HasMaxLength(3).IsRequired();
        builder.Property(x => x.QuoteCurrency).HasMaxLength(3).IsRequired();
        builder.Property(x => x.Rate).HasPrecision(18, 8);
        builder.Property(x => x.BuyRate).HasPrecision(18, 8);
        builder.Property(x => x.SellRate).HasPrecision(18, 8);
        builder.HasIndex(x => new { x.BaseCurrency, x.QuoteCurrency, x.UpdatedAt });
    }
}
