using Limita.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Limita.Infrastructure.Persistence.Configurations;

internal sealed class OtpCodeConfiguration : IEntityTypeConfiguration<OtpCode>
{
    public void Configure(EntityTypeBuilder<OtpCode> builder)
    {
        builder.ToTable("OtpCodes");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.UserId)
            .IsRequired();

        builder.Property(o => o.Purpose)
            .IsRequired();

        builder.Property(o => o.CodeHash)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(o => o.CreatedAt)
            .IsRequired();

        builder.Property(o => o.ExpiresAt)
            .IsRequired();

        builder.Property(o => o.ConsumedAt);

        builder.Property(o => o.FailedAttempts)
            .IsRequired();

        builder.HasIndex(o => o.UserId);
    }
}