using Limita.Application.Common.Abstractions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Limita.Infrastructure.Identity
{
    internal sealed class SecretGenerator : ISecretGenerator
    {
        public string NewRefreshToken()
            => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');

        public string NewOtpCode()
            => RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6", CultureInfo.InvariantCulture);
    }
}
