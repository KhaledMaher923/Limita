<<<<<<< HEAD
using Limita.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Limita.Infrastructure.Persistence.Configurations;

internal sealed class CardConfiguration : IEntityTypeConfiguration<Card>
{
    public void Configure(EntityTypeBuilder<Card> builder)
    {
        builder.ToTable("Cards");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.AccountId)
            .IsRequired();

        builder.Property(c => c.Network)
            .IsRequired();

        builder.Property(c => c.Last4)
            .IsRequired()
            .HasMaxLength(4);

        builder.Property(c => c.ExpiryMonth)
            .IsRequired();

        builder.Property(c => c.ExpiryYear)
            .IsRequired();

        builder.Property(c => c.HolderName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.Status)
            .IsRequired();

        builder.Property(c => c.Token)
            .IsRequired()
            .HasMaxLength(200);

        builder.HasIndex(c => c.AccountId);
        builder.HasIndex(c => c.Token).IsUnique();
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
    internal sealed class CardConfiguration : IEntityTypeConfiguration<Card>
    {
        public void Configure(EntityTypeBuilder<Card> builder)
        {
            builder.ToTable("Cards");
            builder.HasKey(c => c.Id);
            builder.Property(c => c.Id).ValueGeneratedNever();

            builder.Property(c => c.Network).HasConversion<string>().HasMaxLength(20);
            builder.Property(c => c.Status).HasConversion<string>().HasMaxLength(20);
            builder.Property(c => c.Last4).HasMaxLength(4).IsFixedLength().IsUnicode(false).IsRequired();
            builder.Property(c => c.HolderName).HasMaxLength(100).IsRequired();
            builder.Property(c => c.Token).HasMaxLength(200).IsRequired();

            builder.HasIndex(c => c.AccountId);

            builder.HasOne<Account>().WithMany().HasForeignKey(c => c.AccountId).OnDelete(DeleteBehavior.Restrict);
        }
    }
}
>>>>>>> origin/master
