using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Limita.Infrastructure.Messaging
{
    public sealed class SmsOptions
    {
        public const string SectionName = "Sms";

        /// <summary>"Console" (Development only, prints the SMS to the log), "Twilio", or empty (not configured, sending fails).</summary>
        public string Provider { get; init; } = string.Empty;

        public TwilioOptions Twilio { get; init; } = new();

    }

    public sealed class TwilioOptions
    {
        public string AccountSid { get; init; } = string.Empty;

        /// <summary>A secret. Set it with user-secrets or an environment variable, never in a committed file.</summary>
        public string AuthToken { get; init; } = string.Empty;

        /// <summary>The sending phone number or a registered alphanumeric sender ID. Ignored when MessagingServiceSid is set.</summary>
        public string From { get; init; } = string.Empty;

        public string MessagingServiceSid { get; init; } = string.Empty;
    }

}
