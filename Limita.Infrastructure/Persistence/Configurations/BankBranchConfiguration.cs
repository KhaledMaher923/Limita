using Limita.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Limita.Infrastructure.Persistence.Configurations
{
    internal sealed class BankBranchConfiguration : IEntityTypeConfiguration<BankBranch>
    {
        public void Configure(EntityTypeBuilder<BankBranch> builder)
        {
            builder.ToTable("BankBranches");
            builder.HasKey(b => b.Id);
            builder.Property(b => b.Id).ValueGeneratedNever();

            builder.Property(b => b.Name).HasMaxLength(150).IsRequired();
            builder.Property(b => b.Address).HasMaxLength(300).IsRequired();

            builder.HasIndex(b => b.BankId);

            builder.HasOne<Bank>()
                .WithMany()
                .HasForeignKey(b => b.BankId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
