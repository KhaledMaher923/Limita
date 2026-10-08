using Limita.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Limita.Infrastructure.Persistence.Configurations;

internal sealed class WithdrawalConfiguration
    : IEntityTypeConfiguration<Withdrawal>
{
    public void Configure(EntityTypeBuilder<Withdrawal> builder)
    {
        builder.ToTable(
            "Withdrawals",
            table => table.HasCheckConstraint(
                "CK_Withdrawals_Amount_Positive",
                "[Amount] > 0"));

        builder.HasKey(withdrawal => withdrawal.Id);
        builder.Property(withdrawal => withdrawal.Id).ValueGeneratedNever();

        builder.Property(withdrawal => withdrawal.PhoneNumber)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(withdrawal => withdrawal.CodeHash)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(withdrawal => withdrawal.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(withdrawal => withdrawal.FailedVerificationAttempts)
              .IsRequired();

        builder.Property(withdrawal => withdrawal.CreatedAt)
            .IsRequired();

        builder.Property(withdrawal => withdrawal.ExpiresAt)
            .IsRequired();


        builder.HasMoney(
            withdrawal => withdrawal.Amount,
            "Amount",
            "Currency");

        builder.HasIndex(withdrawal => new
        {
            withdrawal.UserId,
            withdrawal.CreatedAt
        });

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(withdrawal => withdrawal.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Account>()
            .WithMany()
            .HasForeignKey(withdrawal => withdrawal.AccountId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}