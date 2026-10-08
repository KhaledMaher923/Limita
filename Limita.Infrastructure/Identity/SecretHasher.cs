using Limita.Application.Common.Abstractions;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Limita.Infrastructure.Identity
{
    /// <summary>
    /// HMAC-SHA256 with a key derived from the JWT secret. A leaked database alone cannot be used to
    /// brute-force the six-digit one-time codes, because the key is not stored in it.
    /// </summary>
    internal sealed class SecretHasher : ISecretHasher
    {
        private readonly byte[] _key;
        public SecretHasher(IOptions<JwtOptions> options)
            => _key = HMACSHA256.HashData(
                Encoding.UTF8.GetBytes(options.Value.SecretKey),
                Encoding.UTF8.GetBytes("limita:secret-hasher:v1"));

        public string Hash(string value)
            => Convert.ToHexString(HMACSHA256.HashData(_key, Encoding.UTF8.GetBytes(value)));


        public bool Verify(string value, string hash)
            => CryptographicOperations.FixedTimeEquals(
                Encoding.ASCII.GetBytes(Hash(value)),
                Encoding.ASCII.GetBytes(hash));
    }
}
