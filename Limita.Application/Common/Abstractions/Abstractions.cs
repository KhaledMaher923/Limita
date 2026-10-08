using Limita.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Limita.Application.Common.Abstractions
{
    public interface IUnitOfWork
    {
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
        Task BeginTransactionAsync(CancellationToken cancellationToken = default);
        Task CommitTransactionAsync(CancellationToken cancellationToken = default);
        Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Handlers query and modify data through this interface. Saving is done by TransactionBehavior,
    /// so handlers add or change entities but never call SaveChanges themselves.
    /// </summary>
    public interface IApplicationDbContext
    {
        DbSet<User> Users { get; }
        DbSet<Account> Accounts { get; }
        DbSet<Card> Cards { get; }
        DbSet<Transaction> Transactions { get; }
        DbSet<Transfer> Transfers { get; }
        DbSet<SpendingLimit> SpendingLimits { get; }
        DbSet<SavingsGoal> SavingsGoals { get; }
        DbSet<Notification> Notifications { get; }
        DbSet<RefreshToken> RefreshTokens { get; }
        DbSet<OtpCode> OtpCodes { get; }
    }

    public interface ICurrentUser
    {
        Guid? UserId { get; }
        bool IsAuthenticated { get; }
    }

}
