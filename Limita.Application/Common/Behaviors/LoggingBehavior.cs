using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Limita.Application.Common.Behaviors
{
    /// <summary>Logs the request name, duration and failures. It never logs request contents (card data, OTPs, tokens).</summary>
    internal sealed class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
        : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            // Requests are nested in their slice (CreateAccount.Command), so log the slice name.
            var name = typeof(TRequest).DeclaringType?.Name ?? typeof(TResponse).Name;
            var stopwatch = Stopwatch.StartNew();
            logger.LogInformation($"Handling {name}");

            try
            {
                var response = await next();

                if (response is Result { IsFailure: true } failed)
                    logger.LogWarning($"{name} failed with {failed.Error.Code}");

                logger.LogInformation($"Handled {name} in { stopwatch.ElapsedMilliseconds} ms");
                return response;
            }
            catch( Exception ex ) 
            {
                logger.LogError(ex, $"{name} threw after {stopwatch.ElapsedMilliseconds} ms");
                throw;
            }


        }
    }
}
