using Limita.Application.Features.Profile.ChangeLanguage;
using Limita.Application.Features.Profile.GetProfile;
using Limita.Application.Features.Profile.UpdateProfile;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Limita.Controllers;

[ApiController]
[Authorize]
[Route("api/me")]
public sealed class ProfileController(ISender sender) : ControllerBase
{
    public sealed record UpdateProfileRequest(string FullName, string? ProfilePictureUrl);
    public sealed record ChangeLanguageRequest(string LanguageCode);

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var profile = await sender.Send(new GetProfileQuery(), ct);
        return profile is null ? NotFound() : Ok(profile);
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateProfileRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new UpdateProfileCommand(request.FullName, request.ProfilePictureUrl), ct);
        return result.IsSuccess ? NoContent() : NotFound(new { result.Error.Code, result.Error.Description });
    }

    [HttpPut("language")]
    public async Task<IActionResult> ChangeLanguage([FromBody] ChangeLanguageRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new ChangeLanguageCommand(request.LanguageCode), ct);
        if (result.IsSuccess) return NoContent();
        return result.Error.Type == Limita.Application.Common.ErrorType.NotFound
            ? NotFound(new { result.Error.Code, result.Error.Description })
            : BadRequest(new { result.Error.Code, result.Error.Description });
    }
}
