using Limita.Application.Common.Abstractions;
using Limita.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Limita.Infrastructure.Identity
{
    internal sealed class JwtTokenGenerator(IOptions<JwtOptions> options) : IJwtTokenGenerator
    {
        private readonly JwtOptions _options = options.Value;

        private readonly SigningCredentials _credentials = new(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Value.SecretKey)),
            SecurityAlgorithms.HmacSha256);

        private readonly JsonWebTokenHandler _handler = new();


        public TimeSpan RefreshTokenLifetime => TimeSpan.FromDays(_options.RefreshTokenDays);

        public AccessToken CreateAccessToken(User user, DateTimeOffset now)
        {
            var expiresAt = now.AddMinutes(_options.ExpiryMinutes);

            var descriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim("sub", user.Id.ToString()),
                    new Claim("email", user.Email),
                    new Claim("name", user.FullName),
                    new Claim("jti", Guid.NewGuid().ToString())
                }),
                Issuer = _options.Issuer,
                Audience = _options.Audience,
                IssuedAt = now.UtcDateTime,
                NotBefore = now.UtcDateTime,
                Expires = expiresAt.UtcDateTime,
                SigningCredentials = _credentials
            };

            return new AccessToken(_handler.CreateToken(descriptor), expiresAt);
        }
    }
}
