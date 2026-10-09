using Limita.Api.Common;
using Limita.Application.Features.Auth;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Limita.Api.Controllers
{
    /// <summary>
    /// Authentication and verification: /api/v1/auth/...
    /// The public actions are throttled per IP address by the "auth" rate limiting policy.
    /// Action names differ from the use-case names (RegisterUser, not Register) so they never hide the Application types.
    /// </summary>
    public sealed class AuthController(ISender sender) : ApiControllerBase
    {
        // ---------- Public ----------

        [HttpPost("register")]
        [AllowAnonymous]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> RegisterUser(Register.Command command, CancellationToken cancellationToken)
            => FromResult(await sender.Send(command, cancellationToken));

        [HttpPost("verify-phone")]
        [AllowAnonymous]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> ConfirmPhone(VerifyPhone.Command command, CancellationToken cancellationToken)
        => FromResult(await sender.Send(command, cancellationToken));

        [HttpPost("resend-otp")]
        [AllowAnonymous]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> ResendCode(ResendOtp.Command command, CancellationToken cancellationToken)
            => FromResult(await sender.Send(command, cancellationToken));

        [HttpPost("login")]
        [AllowAnonymous]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> LoginUser(Login.Command command, CancellationToken cancellationToken)
            => FromResult(await sender.Send(command, cancellationToken));

        [HttpPost("refresh")]
        [AllowAnonymous]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> RefreshTokens(RefreshAccessToken.Command command, CancellationToken cancellationToken)
            => FromResult(await sender.Send(command, cancellationToken));

        [HttpPost("forgot-password")]
        [AllowAnonymous]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> RequestPasswordReset(ForgotPassword.Command command, CancellationToken cancellationToken)
            => FromResult(await sender.Send(command, cancellationToken));

        [HttpPost("reset-password")]
        [AllowAnonymous]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> ResetUserPassword(ResetPassword.Command command, CancellationToken cancellationToken)
            => FromResult(await sender.Send(command, cancellationToken));

        // ---------- Signed in (the base class requires authorization) ----------

        [HttpPost("logout")]
        public async Task<IActionResult> LogoutUser(Logout.Command command, CancellationToken cancellationToken)
            => FromResult(await sender.Send(command, cancellationToken));

        [HttpPost("change-password")]
        public async Task<IActionResult> ChangeUserPassword(ChangePassword.Command command, CancellationToken cancellationToken)
            => FromResult(await sender.Send(command, cancellationToken));

        [HttpGet("me")]
        public async Task<IActionResult> GetMe(CancellationToken cancellationToken)
            => FromResult(await sender.Send(new GetProfile.Query(), cancellationToken));

        [HttpPut("me")]
        public async Task<IActionResult> UpdateMe(UpdateProfile.Command command, CancellationToken cancellationToken)
            => FromResult(await sender.Send(command, cancellationToken));

    }
}
