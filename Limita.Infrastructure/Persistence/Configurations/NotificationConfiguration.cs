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
    internal sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
    {
        public void Configure(EntityTypeBuilder<Notification> builder)
        {
            builder.ToTable("Notifications");
            builder.HasKey(n => n.Id);
            builder.Property(n => n.Id).ValueGeneratedNever();

            builder.Property(n => n.Type).HasConversion<string>().HasMaxLength(30);
            builder.Property(n => n.Title).HasMaxLength(100).IsRequired();
            builder.Property(n => n.Body).HasMaxLength(500).IsRequired();

            builder.HasIndex(n => new { n.UserId, n.CreatedAt });

            builder.HasOne<User>().WithMany().HasForeignKey(n => n.UserId).OnDelete(DeleteBehavior.Restrict);
        }
    }
}
