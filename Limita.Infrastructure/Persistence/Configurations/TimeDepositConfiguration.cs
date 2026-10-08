using Limita.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Limita.Infrastructure.Persistence.Configurations;

internal sealed class TimeDepositConfiguration
    : IEntityTypeConfiguration<TimeDeposit>
{
    public void Configure(EntityTypeBuilder<TimeDeposit> builder)
    {
        builder.ToTable(
            "TimeDeposits",
            table => table.HasCheckConstraint(
                "CK_TimeDeposits_Principal_Positive",
                "[Principal] >= 1000"));

        builder.HasKey(deposit => deposit.Id);
        builder.Property(deposit => deposit.Id).ValueGeneratedNever();

        builder.Property(deposit => deposit.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(deposit => deposit.TermInMonths)
            .IsRequired();

        builder.Property(deposit => deposit.AnnualInterestRatePercent)
            .HasPrecision(5, 2)
            .IsRequired();

        builder.Property(deposit => deposit.CreatedAt)
            .IsRequired();

        builder.Property(deposit => deposit.MaturesAt)
            .IsRequired();

        builder.HasMoney(
            deposit => deposit.Principal,
            "Principal",
            "Currency");

        builder.HasIndex(deposit => new
        {
            deposit.UserId,
            deposit.CreatedAt
        });

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(deposit => deposit.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Account>()
            .WithMany()
            .HasForeignKey(deposit => deposit.FundingAccountId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
