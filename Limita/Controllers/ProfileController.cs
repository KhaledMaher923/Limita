using Limita.Api.Common;
using Limita.Application.Features.Auth;
using Limita.Application.Features.Profile.ChangeLanguage;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Limita.Controllers;

/// <summary>
/// The signed-in user's profile at /api/me. It uses the same Application code as GET/PUT /api/v1/auth/me,
/// so there is a single implementation of "read and change my profile".
/// </summary>
[Route("api/me")]
public sealed class ProfileController(ISender sender) : ApiControllerBase
{
    public sealed record UpdateProfileRequest(string FullName, string? ProfilePictureUrl);
    public sealed record ChangeLanguageRequest(string LanguageCode);

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
        => FromResult(await sender.Send(new GetProfile.Query(), ct));

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateProfileRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new UpdateProfile.Command(request.FullName, request.ProfilePictureUrl), ct);
        return FromResult(result, _ => NoContent());
    }

    [HttpPut("language")]
    public async Task<IActionResult> ChangeLanguage([FromBody] ChangeLanguageRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new ChangeLanguageCommand(request.LanguageCode), ct);
        return FromResult(result, _ => NoContent());
    }
}
