using Limita.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Limita.Infrastructure.Persistence.Configurations
{
    internal sealed class SavingsGoalConfiguration : IEntityTypeConfiguration<SavingsGoal>
    {
        public void Configure(EntityTypeBuilder<SavingsGoal> builder)
        {
            builder.ToTable("SavingsGoals", t => t.HasCheckConstraint("CK_SavingsGoals_Target_Positive", "[TargetAmount] > 0"));
            builder.HasKey(g => g.Id);
            builder.Property(g => g.Id).ValueGeneratedNever();

            builder.Property(g => g.Name).HasMaxLength(100).IsRequired();
            builder.Property(g => g.Status).HasConversion<string>().HasMaxLength(20);
            builder.HasMoney(g => g.TargetAmount, "TargetAmount", "Currency");
            builder.HasMoney(g => g.SavedAmount, "SavedAmount", "SavedCurrency");

            builder.HasIndex(g => g.UserId);

            builder.HasOne<User>().WithMany().HasForeignKey(g => g.UserId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<Account>().WithMany().HasForeignKey(g => g.AccountId).OnDelete(DeleteBehavior.Restrict);
        }
    }
}
