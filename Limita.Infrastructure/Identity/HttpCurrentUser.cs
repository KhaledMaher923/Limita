using Limita.Application.Common.Abstractions;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Limita.Infrastructure.Identity;

internal sealed class HttpCurrentUser(IHttpContextAccessor httpContextAccessor)
    : ICurrentUser
{
    public Guid? UserId
    {
        get
        {
            // JwtBearer can rename "sub" to NameIdentifier, so accept both.
            var user = httpContextAccessor.HttpContext?.User;
            var value = user?.FindFirst("sub")?.Value
                        ?? user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            return Guid.TryParse(value, out var userId)
                ? userId
                : null;
        }
    }

    public bool IsAuthenticated =>
        httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated == true;
}
