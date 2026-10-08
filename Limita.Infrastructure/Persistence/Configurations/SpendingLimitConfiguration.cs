<<<<<<< HEAD
using Limita.Domain.Entities;
using Limita.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Limita.Infrastructure.Persistence.Configurations;

internal sealed class SpendingLimitConfiguration : IEntityTypeConfiguration<SpendingLimit>
{
    public void Configure(EntityTypeBuilder<SpendingLimit> builder)
    {
        builder.ToTable("SpendingLimits");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.UserId)
            .IsRequired();

        builder.Property(s => s.CardId);

        builder.Property(s => s.AccountId);

        builder.Property(s => s.Category);

        builder.Property(s => s.Period)
            .IsRequired();

        builder.OwnsOne(s => s.LimitAmount, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("LimitAmount")
                .HasPrecision(18, 2)
                .IsRequired();

            money.Property(m => m.Currency)
                .HasColumnName("LimitCurrency")
                .HasMaxLength(3)
                .IsRequired();
        });

        builder.Property(s => s.IsActive)
            .IsRequired();

        builder.HasIndex(s => s.UserId);
    }
}
=======
﻿using Limita.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Limita.Infrastructure.Persistence.Configurations
{
    internal sealed class SpendingLimitConfiguration : IEntityTypeConfiguration<SpendingLimit>
    {
        public void Configure(EntityTypeBuilder<SpendingLimit> builder)
        {
            builder.ToTable("SpendingLimits", t =>
            {
                t.HasCheckConstraint("CK_SpendingLimits_Target", "[CardId] IS NOT NULL OR [AccountId] IS NOT NULL");
                t.HasCheckConstraint("CK_SpendingLimits_Amount_Positive", "[LimitAmount] > 0");
            });
            builder.HasKey(s => s.Id);
            builder.Property(s => s.Id).ValueGeneratedNever();

            builder.Property(s => s.Category).HasConversion<string>().HasMaxLength(30);
            builder.Property(s => s.Period).HasConversion<string>().HasMaxLength(20);
            builder.HasMoney(s => s.LimitAmount, "LimitAmount", "Currency");

            builder.HasIndex(s => new { s.UserId, s.IsActive });

            builder.HasOne<User>().WithMany().HasForeignKey(s => s.UserId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<Card>().WithMany().HasForeignKey(s => s.CardId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<Account>().WithMany().HasForeignKey(s => s.AccountId).OnDelete(DeleteBehavior.Restrict);
        }
    }
}
>>>>>>> origin/master
