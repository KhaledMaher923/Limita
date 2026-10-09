using Limita.Api.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Limita.Api.Controllers
{
    public sealed class HealthController : ApiControllerBase
    {
        /// <summary>GET /api/v1/health, public.</summary>
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Get() => Ok(new { status = "healthy" });
    }
}
