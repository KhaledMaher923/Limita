using FluentValidation;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Limita.Application.Common.Behaviors
{
    /// <summary>Runs every FluentValidation validator for the request. Invalid input throws ValidationException (HTTP 400).</summary>
    internal sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
        : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            var validatorList = validators.ToList();
            if(validatorList.Count == 0)
                return await next();

            var context = new ValidationContext<TRequest>(request);
            var results = await Task.WhenAll(validatorList.Select(v => v.ValidateAsync(context, cancellationToken)));
            var failures = results.SelectMany(r => r.Errors).Where(f => f is not null).ToList();

            if (failures.Count > 0)
                throw new ValidationException(failures);

            return await next();
        }
    }
}
