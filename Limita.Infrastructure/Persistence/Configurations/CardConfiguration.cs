using Limita.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Limita.Infrastructure.Persistence.Configurations;

internal sealed class CardConfiguration : IEntityTypeConfiguration<Card>
{
    public void Configure(EntityTypeBuilder<Card> builder)
    {
        builder.ToTable("Cards");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.AccountId)
            .IsRequired();

        builder.Property(c => c.Network)
            .IsRequired();

        builder.Property(c => c.Last4)
            .IsRequired()
            .HasMaxLength(4);

        builder.Property(c => c.ExpiryMonth)
            .IsRequired();

        builder.Property(c => c.ExpiryYear)
            .IsRequired();

        builder.Property(c => c.HolderName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.Status)
            .IsRequired();

        builder.Property(c => c.Token)
            .IsRequired()
            .HasMaxLength(200);

        builder.HasIndex(c => c.AccountId);
        builder.HasIndex(c => c.Token).IsUnique();
    }
}