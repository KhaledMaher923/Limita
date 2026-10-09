using Limita.Application.Common.Abstractions;
using Limita.Infrastructure.Identity;
using Limita.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace Limita.Controllers;

[ApiController]
[Route("api/dev")]
public sealed class DevelopmentSessionController(
    ApplicationDbContext dbContext,
    IJwtTokenGenerator tokenGenerator,
    IHostEnvironment environment,
    TimeProvider timeProvider) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("session")]
    public async Task<IActionResult> CreateSession(
        CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment())
            return NotFound();

        const string email = "dev.user@limita.local";

        var user = await dbContext.Users
            .SingleOrDefaultAsync(
                item => item.Email == email,
                cancellationToken);

        if (user is null)
            return NotFound("Development user was not seeded.");

        var account = await dbContext.Accounts
            .SingleOrDefaultAsync(
                item => item.UserId == user.Id,
                cancellationToken);

        if (account is null)
            return NotFound("Development account was not seeded.");

        var accessToken = tokenGenerator.CreateAccessToken(
            user,
            timeProvider.GetUtcNow());

        return Ok(new
        {
            userId = user.Id,
            accountId = account.Id,
            token = accessToken.Value,
            expiresAt = accessToken.ExpiresAt
        });
    }
}