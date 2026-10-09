using Limita.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Limita.Infrastructure.Persistence.Configurations;

public sealed class LanguageConfiguration : IEntityTypeConfiguration<Language>
{
    public void Configure(EntityTypeBuilder<Language> builder)
    {
        builder.ToTable("Languages");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(10).IsRequired();
        builder.Property(x => x.NativeName).HasMaxLength(60).IsRequired();
        builder.Property(x => x.EnglishName).HasMaxLength(60).IsRequired();
        builder.HasIndex(x => x.Code).IsUnique();
        builder.HasData(
            new { Id = Guid.Parse("3c0a32d6-49bc-4c0a-b70f-b0d3c7ac0001"), Code = "en", NativeName = "English", EnglishName = "English", IsRightToLeft = false, IsEnabled = true },
            new { Id = Guid.Parse("3c0a32d6-49bc-4c0a-b70f-b0d3c7ac0002"), Code = "ar", NativeName = "العربية", EnglishName = "Arabic", IsRightToLeft = true, IsEnabled = true });
    }
}
