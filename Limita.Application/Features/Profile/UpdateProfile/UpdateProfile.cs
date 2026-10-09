using FluentValidation;
using Limita.Application.Common;
using Limita.Application.Common.Abstractions;
using Limita.Application.Common.Messaging;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Limita.Application.Features.Profile.UpdateProfile;

public sealed record UpdateProfileCommand(string FullName, string? ProfilePictureUrl) : ICommand<bool>;

public sealed class UpdateProfileHandler(IApplicationDbContext db, ICurrentUser currentUser)
    : ICommandHandler<UpdateProfileCommand, bool>
{
    public async Task<Result<bool>> Handle(UpdateProfileCommand request, CancellationToken ct)
    {
        if (currentUser.UserId is not Guid userId) throw new UnauthorizedAccessException();
        var user = await db.Users.FirstOrDefaultAsync(x => x.Id == userId, ct);
        if (user is null) return Result.Failure<bool>(Error.NotFound("Users.NotFound", "User profile was not found."));
        user.UpdateProfile(request.FullName, request.ProfilePictureUrl);
        return Result.Success(true);
    }
}
public sealed class UpdateProfileCommandValidator : FluentValidation.AbstractValidator<UpdateProfileCommand>
{
    public UpdateProfileCommandValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.ProfilePictureUrl).MaximumLength(2048);
    }
}

