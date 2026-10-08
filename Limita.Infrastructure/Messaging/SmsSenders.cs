using Limita.Application.Common.Abstractions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Limita.Infrastructure.Messaging
{
    /// <summary>
    /// Development only: writes the SMS (including the one-time code) to the log instead of sending it.
    /// It is registered by the API only when the environment is Development.
    /// </summary>
    public sealed class DevelopmentSmsSender(ILogger<DevelopmentSmsSender> logger) : ISmsSender
    {
        public Task SendAsync(string phoneNumber, string message, CancellationToken cancellationToken = default)
        {
            logger.LogWarning("DEVELOPMENT SMS to {PhoneNumber}: {Message}", phoneNumber, message);
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// The default outside Development. It fails loudly so the app cannot go live without a real SMS provider.
    /// Replace it with an implementation for your provider.
    /// </summary>
    internal sealed class NotConfiguredSmsSender : ISmsSender
    {
        public Task SendAsync(string phoneNumber, string message, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("No SMS provider is configured. Register an ISmsSender implementation.");
    }

}
