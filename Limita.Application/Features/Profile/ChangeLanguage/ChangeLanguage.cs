using FluentValidation;
using Limita.Application.Common;
using Limita.Application.Common.Abstractions;
using Limita.Application.Common.Messaging;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Limita.Application.Features.Profile.ChangeLanguage;

public sealed record ChangeLanguageCommand(string LanguageCode) : ICommand<bool>;

public sealed class ChangeLanguageHandler(IApplicationDbContext db, ICurrentUser currentUser)
    : ICommandHandler<ChangeLanguageCommand, bool>
{
    public async Task<Result<bool>> Handle(ChangeLanguageCommand request, CancellationToken ct)
    {
        if (currentUser.UserId is not Guid userId)
            return Result.Failure<bool>(Error.Unauthorized("Auth.Unauthenticated", "You must be signed in."));

        var code = request.LanguageCode.Trim().ToLowerInvariant(); // the validator guarantees it is not empty
        var languageExists = await db.Languages.AnyAsync(x => x.Code == code && x.IsEnabled, ct);

        if (!languageExists) return Result.Failure<bool>(Error.Validation("Languages.Unsupported", "Language code is not supported."));
        var user = await db.Users.FirstOrDefaultAsync(x => x.Id == userId, ct);

        if (user is null) return Result.Failure<bool>(Error.NotFound("Users.NotFound", "User profile was not found."));
        user.ChangePreferredLanguage(code);
        return Result.Success(true);
    }
}
public sealed class ChangeLanguageCommandValidator : AbstractValidator<ChangeLanguageCommand>
{
    public ChangeLanguageCommandValidator() => RuleFor(x => x.LanguageCode).NotEmpty().MaximumLength(10);
}

