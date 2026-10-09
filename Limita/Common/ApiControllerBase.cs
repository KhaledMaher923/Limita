using Limita.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Limita.Api.Common
{
    /// <summary>
    /// Base class for every controller.
    /// Routes start with /api/v1/[controller]. Every action requires a signed-in user unless it is marked [AllowAnonymous],
    /// so a new controller is secure by default. It also turns a Result from the Application layer into an HTTP response.
    /// </summary>
    [ApiController]
    [Authorize]
    [Route("api/v1/[controller]")]
    public abstract class ApiControllerBase : ControllerBase
    {
        /// <summary>Success becomes 204 (or the response you pass in), a failed Result becomes a problem response.</summary>
        protected IActionResult FromResult(Result result, Func<IActionResult>? onSuccess = null)
            => result.IsSuccess
                ? (onSuccess?.Invoke() ?? NoContent())
                : ErrorResponse(result.Error);

        /// <summary>Success becomes 200 with the value (or the response you pass in), a failed Result becomes a problem response.</summary>
        protected IActionResult FromResult<T>(Result<T> result, Func<T, IActionResult>? onSuccess = null)
            => result.IsSuccess
                ? (onSuccess?.Invoke(result.Value) ?? Ok(result.Value))
                : ErrorResponse(result.Error);


        private ObjectResult ErrorResponse(Error error)
        {
            var status = error.Type switch
            {
                ErrorType.Validation => StatusCodes.Status400BadRequest,
                ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
                ErrorType.Forbidden => StatusCodes.Status403Forbidden,
                ErrorType.NotFound => StatusCodes.Status404NotFound,
                ErrorType.Conflict => StatusCodes.Status409Conflict,
                ErrorType.BusinessRule => StatusCodes.Status422UnprocessableEntity,
                ErrorType.TooManyRequests => StatusCodes.Status429TooManyRequests,
                _ => StatusCodes.Status500InternalServerError
            };

            return Problem(statusCode: status, title: error.Code, detail: error.Description);
        }
    }
}
