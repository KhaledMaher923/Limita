using FluentValidation;
using Limita.Application.Common.Abstractions;
using Limita.Domain.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Limita.Api.Middleware
{
    public sealed class GlobalExceptionHandler(
        IProblemDetailsService problemDetailsService,
        ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
    {
        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            var problem = exception switch
            {
                ValidationException validation => new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Validation failed",
                    Extensions =
                    {
                        ["errors"] = validation.Errors
                            .GroupBy(e => e.PropertyName)
                            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())
                    }
                },
                DomainException domain => new ProblemDetails
                {
                    Status = StatusCodes.Status422UnprocessableEntity,
                    Title = "Business rule violated",
                    Detail = domain.Message
                },
                SmsDeliveryException => new ProblemDetails
                {
                    Status = StatusCodes.Status503ServiceUnavailable,
                    Title = "SMS delivery failed",
                    Detail = "We could not send the verification code. Please try again shortly."
                },
                // A unique index was violated (SQL Server error 2601 or 2627). This is how two requests
                // that race past the "already exists" checks end up: one wins, the other gets a conflict.
                DbUpdateException { InnerException: SqlException { Number: 2601 or 2627 } } duplicate => new ProblemDetails
                {
                    Status = StatusCodes.Status409Conflict,
                    Title = "Conflict",
                    Detail = ConflictDetail(duplicate)
                },
                _ => new ProblemDetails
                {
                    Status = StatusCodes.Status500InternalServerError,
                    Title = "An unexpected error occurred"
                }
            };

            if (problem.Status == StatusCodes.Status500InternalServerError)
                logger.LogError(exception, "Unhandled exception");

            httpContext.Response.StatusCode = problem.Status!.Value;

            return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = httpContext,
                Exception = exception,
                ProblemDetails = problem
            });
        }
        
        private static string ConflictDetail(DbUpdateException exception)
        {
            var message = exception.InnerException?.Message ?? string.Empty;

            if (message.Contains("IX_Users_Email")) return "An account with this email already exists.";
            if (message.Contains("IX_Users_PhoneNumber")) return "An account with this phone number already exists.";
            if (message.Contains("IX_Transfers_IdempotencyKey")) return "This transfer was already submitted.";

            return "The resource already exists.";
        }
    }
}
