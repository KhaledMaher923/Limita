using Limita.Application.Common.Abstractions;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Limita.Application.Common.Behaviors
{
    /// <summary>
    /// Wraps every command in a database transaction. A failed Result rolls back; a success saves and commits.
    /// Rule: a command handler must not send another command through MediatR (transactions do not nest).
    /// </summary>
    internal sealed class TransactionBehavior<TRequest, TResponse>(IUnitOfWork unitOfWork)
        : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull, IRequest<TResponse>, ICommand
        where TResponse : Result
    {
        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            await unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                var response = await next();

                if (response.IsFailure)
                {
                    await unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return response;
                }

                await unitOfWork.SaveChangesAsync(cancellationToken);
                await unitOfWork.CommitTransactionAsync(cancellationToken);
                return response;
            }
            catch
            {
                await unitOfWork.RollbackTransactionAsync(CancellationToken.None);
                throw;
            }


        }
    }
}
