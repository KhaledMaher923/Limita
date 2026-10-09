using Limita.Application.Common.Abstractions;
using System.Security.Claims;

namespace Limita.Api.Services
{
    public sealed class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
    {
        public Guid? UserId
        {
            get
            {
                var user = accessor.HttpContext?.User;
                var value = user?.FindFirstValue("sub") ?? user?.FindFirstValue(ClaimTypes.NameIdentifier);
                return Guid.TryParse(value, out var id) ? id : null;
            }
        }

        public bool IsAuthenticated => accessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;
    }
}
