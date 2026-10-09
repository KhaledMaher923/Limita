using Limita.Application.Common;
using Limita.Application.Common.Abstractions;
using Limita.Application.Common.Messaging;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Limita.Application.Features.Auth
{
    public static class GetProfile
    {
        public sealed record Query : IQuery<UserProfile>;

        internal sealed class Handler(IApplicationDbContext db, ICurrentUser currentUser) : IQueryHandler<Query, UserProfile>
        {
            public async Task<Result<UserProfile>> Handle(Query request, CancellationToken cancellationToken)
            {
                if (currentUser.UserId is not { } userId)
                    return AuthErrors.Unauthenticated;

                var profile = await db.Users
                    .AsNoTracking()
                    .Where(u => u.Id == userId)
                    .Select(u => new UserProfile(
                        u.Id,
                        u.FullName,
                        u.Email,
                        u.PhoneNumber,
                        u.IsPhoneVerified,
                        u.ProfilePictureUrl,
                        u.CreatedAt))
                    .FirstOrDefaultAsync(cancellationToken);

                if (profile is null)
                    return AuthErrors.UserNotFound;

                return profile;
            }
        }
    }
}
