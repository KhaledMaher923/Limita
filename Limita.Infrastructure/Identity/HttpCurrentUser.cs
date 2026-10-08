using Limita.Application.Common.Abstractions;
using Microsoft.AspNetCore.Http;

namespace Limita.Infrastructure.Identity;

internal sealed class HttpCurrentUser(IHttpContextAccessor httpContextAccessor)
    : ICurrentUser
{
    public Guid? UserId
    {
        get
        {
            var value = httpContextAccessor.HttpContext?
                .User
                .FindFirst("sub")?
                .Value;

            return Guid.TryParse(value, out var userId)
                ? userId
                : null;
        }
    }

    public bool IsAuthenticated =>
        httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated == true;
}