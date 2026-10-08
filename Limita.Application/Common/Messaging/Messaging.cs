using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Limita.Application.Common.Messaging
{
    /// <summary>Marker used by the transaction behavior: only commands run inside a database transaction.</summary>
    public interface ICommandBase { }

    /// <summary>
    /// Marker for commands whose database changes must be saved even when they return a failed Result
    /// (for example, counting a wrong verification code so the code can be locked after too many attempts).
    /// </summary>
    public interface ICommitOnFailure { }

    public interface ICommand : IRequest<Result>, ICommandBase { }

    public interface ICommand<TResponse> : IRequest<Result<TResponse>>, ICommandBase { }

    public interface IQuery<TResponse> : IRequest<Result<TResponse>> { }

    public interface ICommandHandler<in TCommand> : IRequestHandler<TCommand, Result>
    where TCommand : ICommand { }

    public interface ICommandHandler<in TCommand, TResponse> : IRequestHandler<TCommand, Result<TResponse>>
    where TCommand : ICommand<TResponse> { }

    public interface IQueryHandler<in TQuery, TResponse> : IRequestHandler<TQuery, Result<TResponse>>
    where TQuery : IQuery<TResponse> { }
}
