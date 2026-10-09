using FluentValidation;
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
    /// <summary>Updates the name and profile picture. Changing the phone number needs its own verified flow and is not part of this command.</summary>
    public static class UpdateProfile
    {
        public sealed record Command(string FullName, string? ProfilePictureUrl) : ICommand<UserProfile>;

        internal sealed class Validator : AbstractValidator<Command>
        {
            public Validator()
            {
                RuleFor(x => x.FullName).NotEmpty().MaximumLength(100);
                RuleFor(x => x.ProfilePictureUrl)
                    .MaximumLength(2048)
                    .Must(url => Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps)
                    .When(x => !string.IsNullOrWhiteSpace(x.ProfilePictureUrl))
                    .WithMessage("The profile picture must be an https URL.");
            }
        }

        internal sealed class Handler(IApplicationDbContext db, ICurrentUser currentUser)
            : ICommandHandler<Command, UserProfile>
        {
            public async Task<Result<UserProfile>> Handle(Command request, CancellationToken cancellationToken)
            {
                if (currentUser.UserId is not { } userId)
                    return AuthErrors.Unauthenticated;

                var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
                if (user is null)
                    return AuthErrors.UserNotFound;

                user.UpdateProfile(request.FullName, request.ProfilePictureUrl);

                return UserProfile.From(user);
            }
        }

    }
}
