using Limita.Application.Common.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Limita.Application.Features.Profile.GetProfile;

public sealed record GetProfileQuery : IRequest<UserProfileDto?>;
public sealed record UserProfileDto(Guid Id, string FullName, string Email, string PhoneNumber, bool IsPhoneVerified,
    string? ProfilePictureUrl, string PreferredLanguageCode, DateTimeOffset CreatedAt);

public sealed class GetProfileHandler(IApplicationDbContext db, ICurrentUser currentUser)
    : IRequestHandler<GetProfileQuery, UserProfileDto?>
{
    public async Task<UserProfileDto?> Handle(GetProfileQuery request, CancellationToken ct)
    {
        if (currentUser.UserId is not Guid userId) throw new UnauthorizedAccessException();
        return await db.Users.AsNoTracking().Where(x => x.Id == userId)
            .Select(x => new UserProfileDto(x.Id, x.FullName, x.Email, x.PhoneNumber, x.IsPhoneVerified,
                x.ProfilePictureUrl, x.PreferredLanguageCode, x.CreatedAt)).FirstOrDefaultAsync(ct);
    }
}
