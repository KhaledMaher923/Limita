using Limita.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Limita.Domain.Entities
{
    public sealed class User : Entity
    {
        private User() // required by EF Core
        {
        }

        public string FullName { get; private set; } = string.Empty;
        public string Email { get; private set; } = string.Empty;
        public string PhoneNumber { get; private set; } = string.Empty;
        public string PasswordHash { get; private set; } = string.Empty;
        public bool IsPhoneVerified { get; private set; }
        public string? ProfilePictureUrl { get; private set; }
        public DateTimeOffset CreatedAt { get; private set; }

        public const int MaxFailedLoginAttempts = 5;
        public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

        public int FailedLoginAttempts { get; private set; }
        public DateTimeOffset? LockedUntil { get; private set; }

        public static User Register(string fullName, string email, string phoneNumber, string passwordHash, DateTimeOffset now) => new()
        {
            FullName = Guard.NotBlank(fullName, "Full name", 100),
            Email = Guard.NotBlank(email, "Email", 256).ToLowerInvariant(),
            PhoneNumber = Guard.NotBlank(phoneNumber, "Phone number", 20),
            PasswordHash = Guard.NotBlank(passwordHash, "Password hash", 256),
            CreatedAt = now
        };
        public void VerifyPhone() => IsPhoneVerified = true;

        public bool IsLockedOut(DateTimeOffset now) => LockedUntil is { } until && until > now;

        /// <summary>Counts a wrong password. After MaxFailedLoginAttempts the account is locked for LockoutDuration.</summary>
        public void RegisterFailedLogin(DateTimeOffset now)
        {
            FailedLoginAttempts++;

            if (FailedLoginAttempts >= MaxFailedLoginAttempts)
            {
                LockedUntil = now.Add(LockoutDuration);
                FailedLoginAttempts = 0;
            }
        }

        public void RegisterSuccessfulLogin() => ClearLockout();

        public void ClearLockout()
        {
            FailedLoginAttempts = 0;
            LockedUntil = null;
        }

        public void UpdateProfile(string fullName, string? profilePictureUrl)
        {
            FullName = Guard.NotBlank(fullName, "Full name", 100);
            ProfilePictureUrl = Guard.Optional(profilePictureUrl, "Profile picture URL", 2048);
        }

        public void ChangePassword(string newPasswordHash)
            => PasswordHash = Guard.NotBlank(newPasswordHash, "Password hash", 256);

    }
}
