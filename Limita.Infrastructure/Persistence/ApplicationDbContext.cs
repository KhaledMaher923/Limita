using Limita.Application.Common.Abstractions;
using Limita.Application.Common.DomainEvents;
using Limita.Domain.Common;
using Limita.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace Limita.Infrastructure.Persistence
{
    public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IPublisher publisher)
        : DbContext(options), IApplicationDbContext, IUnitOfWork
    {
        private IDbContextTransaction? _transaction;

        public DbSet<Account> Accounts => Set<Account>();
        public DbSet<Card> Cards => Set<Card>();
        public DbSet<Transaction> Transactions => Set<Transaction>();
        public DbSet<Transfer> Transfers => Set<Transfer>();
        public DbSet<User> Users => Set<User>();
        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
        public DbSet<OtpCode> OtpCodes => Set<OtpCode>();
        public DbSet<Notification> Notifications => Set<Notification>();
        public DbSet<SpendingLimit> SpendingLimits => Set<SpendingLimit>();
        public DbSet<SavingsGoal> SavingsGoals => Set<SavingsGoal>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Picks up every IEntityTypeConfiguration<T> in this assembly (one per entity, added per module).
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        }

        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            // Every decimal in the model is money-shaped unless a configuration says otherwise.
            configurationBuilder.Properties<decimal>().HavePrecision(18, 2);
        }

        // Use the async API only. Domain events are dispatched BEFORE saving, so event handlers
        // take part in the same transaction as the command that raised them.
        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            await DispatchDomainEventsAsync(cancellationToken);
            return await base.SaveChangesAsync(cancellationToken);
        }

        public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
            => _transaction ??= await Database.BeginTransactionAsync(cancellationToken);

        public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
        {
            if (_transaction is null) return;
            await _transaction.CommitAsync(cancellationToken);
            await DisposeTransactionAsync();
        }

        public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
        {
            if (_transaction is null) return;
            await _transaction.RollbackAsync(cancellationToken);
            await DisposeTransactionAsync();
        }

        private async Task DisposeTransactionAsync()
        {
            if (_transaction is not null)
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        private async Task DispatchDomainEventsAsync(CancellationToken cancellationToken)
        {
            while (true)
            {
                var entities = ChangeTracker.Entries<Entity>()
                    .Select(entry => entry.Entity)
                    .Where(entity => entity.DomainEvents.Count > 0)
                    .ToList();

                if (entities.Count == 0) return;

                var events = entities.SelectMany(entity => entity.DomainEvents).ToList();
                entities.ForEach(entity => entity.ClearDomainEvents());

                foreach (var domainEvent in events)
                    await publisher.Publish(DomainEventNotification.Create(domainEvent), cancellationToken);
            }
        }

    }
}