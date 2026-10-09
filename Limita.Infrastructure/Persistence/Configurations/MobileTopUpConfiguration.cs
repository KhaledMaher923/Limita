using Limita.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Limita.Infrastructure.Persistence.Configurations
{
    internal sealed class MobileTopUpConfiguration : IEntityTypeConfiguration<MobileTopUp>
    {
        public void Configure(EntityTypeBuilder<MobileTopUp> builder)
        {
            builder.ToTable("MobileTopUps", t => t.HasCheckConstraint("CK_MobileTopUps_Amount_Positive", "[Amount] > 0"));
            builder.HasKey(t => t.Id);
            builder.Property(t => t.Id).ValueGeneratedNever();

            builder.Property(t => t.PhoneNumber).HasMaxLength(20).IsRequired();
            builder.Property(t => t.IdempotencyKey).HasMaxLength(100).IsRequired();
            builder.HasMoney(t => t.Amount, "Amount", "Currency");

            builder.HasIndex(t => new { t.UserId, t.IdempotencyKey }).IsUnique();
            builder.HasIndex(t => new { t.UserId, t.CreatedAt });

            builder.HasOne<User>().WithMany().HasForeignKey(t => t.UserId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<Account>().WithMany().HasForeignKey(t => t.AccountId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<Transaction>().WithMany().HasForeignKey(t => t.TransactionId).OnDelete(DeleteBehavior.Restrict);
        }
    }
}
