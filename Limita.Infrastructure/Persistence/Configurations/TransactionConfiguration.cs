using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Limita.Domain.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Limita.Infrastructure.Persistence.Configurations
{
    internal sealed class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
    {
        public void Configure(EntityTypeBuilder<Transaction> builder)
        {
            builder.ToTable("Transactions", t => t.HasCheckConstraint("CK_Transactions_Amount_Positive", "[Amount] > 0"));
            builder.HasKey(t => t.Id);
            builder.Property(t => t.Id).ValueGeneratedNever();

            builder.Property(t => t.Type).HasConversion<string>().HasMaxLength(10);
            builder.Property(t => t.Category).HasConversion<string>().HasMaxLength(30);
            builder.Property(t => t.Description).HasMaxLength(200);
            builder.HasMoney(t => t.Amount, "Amount", "Currency");

            // Transaction history and spending reports filter by account and date.
            builder.HasIndex(t => new { t.AccountId, t.CreatedAt });
            builder.HasIndex(t => t.TransferId);
            builder.HasIndex(t => t.SavingsGoalId);

            builder.HasOne<Account>().WithMany().HasForeignKey(t => t.AccountId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<Transfer>().WithMany().HasForeignKey(t => t.TransferId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<SavingsGoal>().WithMany().HasForeignKey(t => t.SavingsGoalId).OnDelete(DeleteBehavior.Restrict);
        }
    }
}
