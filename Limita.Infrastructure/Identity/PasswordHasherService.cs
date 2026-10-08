using Limita.Application.Common.Abstractions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Limita.Infrastructure.Identity
{
    /// <summary>
    /// Password hashing with the ASP.NET Core Identity hasher (PBKDF2, random salt, versioned format).
    /// The hasher ignores the user object, so a plain object is passed.
    /// </summary>
    internal sealed class PasswordHasherService : IPasswordHasher
    {
        private static readonly object Subject = new();
        private readonly PasswordHasher<object> _hasher =
            new(Options.Create(new PasswordHasherOptions { IterationCount = 210_000 }));

        private readonly string _dummyHash;
        public PasswordHasherService()
            => _dummyHash = _hasher.HashPassword(Subject, "limita-dummy-password-used-for-timing");

        public string Hash(string password) => _hasher.HashPassword(Subject, password);
        public bool Verify(string password, string hash)
            => _hasher.VerifyHashedPassword(Subject, hash, password) != PasswordVerificationResult.Failed;

        public void SimulateVerification(string password)
            => _hasher.VerifyHashedPassword(Subject, _dummyHash, password);

    }
}
