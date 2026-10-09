using Limita.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Limita.Infrastructure.Persistence.Configurations
{
    internal sealed class BeneficiaryConfiguration : IEntityTypeConfiguration<Beneficiary>
    {
        public void Configure(EntityTypeBuilder<Beneficiary> builder)
        {
            builder.ToTable("Beneficiaries");
            builder.HasKey(b => b.Id);
            builder.Property(b => b.Id).ValueGeneratedNever();

            builder.Property(b => b.Name).HasMaxLength(100).IsRequired();
            builder.Property(b => b.TransferType).HasConversion<string>().HasMaxLength(20);
            builder.Property(b => b.CardNumberLast4).HasMaxLength(4);
            builder.Property(b => b.CardNumberHash).HasMaxLength(256);
            builder.Property(b => b.AccountNumber).HasMaxLength(30);

            // MaskedCardNumber is a computed property, not mapped
            builder.Ignore(b => b.MaskedCardNumber);

            builder.HasIndex(b => b.UserId);

            builder.HasOne<User>()
                .WithMany()
                .HasForeignKey(b => b.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<Bank>()
                .WithMany()
                .HasForeignKey(b => b.BankId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<BankBranch>()
                .WithMany()
                .HasForeignKey(b => b.BranchId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
