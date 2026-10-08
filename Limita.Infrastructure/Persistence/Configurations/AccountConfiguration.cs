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
    internal sealed class AccountConfiguration : IEntityTypeConfiguration<Account>
    {
        public void Configure(EntityTypeBuilder<Account> builder)
        {
            builder.ToTable("Accounts", t => t.HasCheckConstraint("CK_Accounts_Balance_NonNegative", "[Balance] >= 0"));
            builder.HasKey(a => a.Id);
            builder.Property(a => a.Id).ValueGeneratedNever();

            builder.Property(a => a.AccountNumber).HasMaxLength(34).IsRequired();
            builder.Property(a => a.Type).HasConversion<string>().HasMaxLength(20);
            builder.Property(a => a.Status).HasConversion<string>().HasMaxLength(20);
            builder.Property(a => a.RowVersion).IsRowVersion();
            builder.HasMoney(a => a.Balance, "Balance", "Currency");

            builder.HasIndex(a => a.AccountNumber).IsUnique();
            builder.HasIndex(a => a.UserId);

            builder.HasOne<User>().WithMany().HasForeignKey(a => a.UserId).OnDelete(DeleteBehavior.Restrict);
        }
    }
}
