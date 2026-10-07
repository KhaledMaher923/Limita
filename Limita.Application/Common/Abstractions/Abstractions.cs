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
    /// Handlers query and modify data through this interface. Add a DbSet property here
    /// as each module introduces its entities (for example: DbSet&lt;Account&gt; Accounts).
    /// </summary>
    public interface IApplicationDbContext { }

    public interface ICurrentUser
    {
        Guid? UserId { get; }
        bool IsAuthenticated { get; }
    }

}
