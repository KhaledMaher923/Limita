using FluentValidation;
using Limita.Application.Common;
using Limita.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Limita.Application.Features.Auth
{
    internal static class AuthSettings
    {
        public static readonly TimeSpan OtpLifetime = TimeSpan.FromMinutes(5);
        public static readonly TimeSpan OtpResendCooldown = TimeSpan.FromSeconds(60);
    }

    internal static class AuthErrors
    {
        public static readonly Error Unauthenticated = 
            Error.Unauthorized("Auth.Unauthenticated", "You must be signed in.");
        public static readonly Error InvalidCredentials =
            Error.Unauthorized("Auth.InvalidCredentials", "The email or password is incorrect.");

        public static readonly Error PhoneNotVerified =
            Error.Forbidden("Auth.PhoneNotVerified", "Verify your phone number before signing in.");

        public static readonly Error EmailTaken =
            Error.Conflict("Auth.EmailTaken", "An account with this email already exists.");

        public static readonly Error PhoneTaken =
            Error.Conflict("Auth.PhoneTaken", "An account with this phone number already exists.");

        public static readonly Error AccountLocked =
            Error.TooManyRequests("Auth.AccountLocked", "Too many failed sign-in attempts. Try again in 15 minutes.");

        public static readonly Error InvalidCode =
            Error.Validation("Auth.InvalidCode", "The code is invalid or has expired.");

        public static readonly Error OtpCooldown =
            Error.TooManyRequests("Auth.OtpCooldown", "Please wait a minute before requesting another code.");

        public static readonly Error InvalidRefreshToken =
            Error.Unauthorized("Auth.InvalidRefreshToken", "The refresh token is invalid or has expired.");

        public static readonly Error CurrentPasswordIncorrect =
            Error.Validation("Auth.CurrentPasswordIncorrect", "The current password is incorrect.");

        public static readonly Error UserNotFound =
            Error.NotFound("Auth.UserNotFound", "The user was not found.");

    }

    internal static class AuthInput
    {
        public static readonly Regex PhoneRegex = new(@"^\+[1-9]\d{9,14}$", RegexOptions.Compiled);

        public static string NormalizeEmail(string value) => value.Trim().ToLowerInvariant();

        /// <summary>Removes spaces, dashes and brackets: "+20 100-123 4567" becomes "+201001234567".</summary>
        public static string NormalizePhone(string? value)
            => value is null ? string.Empty : Regex.Replace(value.Trim(), @"[\s\-()]", string.Empty);

    }

    internal static class AuthValidation
    {
        public static IRuleBuilderOptions<T, string> StrongPassword<T>(this IRuleBuilder<T, string> rule) => rule
            .NotEmpty()
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
            .MaximumLength(128).WithMessage("Password cannot exceed 128 characters.")
            .Matches("[A-Z]").WithMessage("Password must contain an uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain a lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain a digit.");

        public static IRuleBuilderOptions<T, string> ValidPhoneNumber<T>(this IRuleBuilder<T, string> rule) => rule
            .NotEmpty()
            .Must(phone => AuthInput.PhoneRegex.IsMatch(AuthInput.NormalizePhone(phone)))
            .WithMessage("Phone number must be in international format, for example +201001234567.");

        public static IRuleBuilderOptions<T, string> ValidOtpCode<T>(this IRuleBuilder<T, string> rule) => rule
            .NotEmpty()
            .Matches(@"^\d{6}$").WithMessage("The code must be six digits.");

    }

    /// <summary>The access and refresh tokens returned after signing in or refreshing.</summary>
    public sealed record AuthTokens(
        string AccessToken,
        DateTimeOffset AccessTokenExpiresAt,
        string RefreshToken,
        DateTimeOffset RefreshTokenExpiresAt);

    public sealed record UserProfile(
        Guid Id,
        string FullName,
        string Email,
        string PhoneNumber,
        bool IsPhoneVerified,
        string? ProfilePictureUrl,
        DateTimeOffset CreatedAt)
    {
        public static UserProfile From(User user) => new(
            user.Id,
            user.FullName,
            user.Email,
            user.PhoneNumber,
            user.IsPhoneVerified,
            user.ProfilePictureUrl,
            user.CreatedAt);
    }










}
