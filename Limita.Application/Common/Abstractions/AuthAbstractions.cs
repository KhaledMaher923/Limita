using Limita.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Limita.Application.Common.Abstractions
{
    public sealed record AccessToken(string Value, DateTimeOffset ExpiresAt);

    public interface IPasswordHasher
    {
        string Hash(string password);
        bool Verify(string password, string hash);

        /// <summary>Spends the same time as a real check, so an unknown email cannot be told apart from a wrong password by timing.</summary>
        void SimulateVerification(string password);
    }

    public interface IJwtTokenGenerator
    {
        AccessToken CreateAccessToken(User user, DateTimeOffset now);
        TimeSpan RefreshTokenLifetime { get; }
    }
    public interface ISecretGenerator
    {
        /// <summary>A random, URL-safe refresh token (256 bits).</summary>
        string NewRefreshToken();

        /// <summary>A random six-digit one-time code.</summary>
        string NewOtpCode();
    }
    public interface ISecretHasher
    {
        string Hash(string value);
        bool Verify(string value, string hash);
    }

    /// <summary>Thrown when the SMS provider cannot be reached or rejects a message. The API answers 503.</summary>
    public sealed class SmsDeliveryException : Exception
    {
        public SmsDeliveryException(string message, Exception? innerException = null) : base(message, innerException)
        {
        }
    }

    public interface ISmsSender
    {
        Task SendAsync(string phoneNumber, string message, CancellationToken cancellationToken = default);
    }


}
