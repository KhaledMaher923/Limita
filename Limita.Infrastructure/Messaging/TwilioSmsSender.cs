using Limita.Application.Common.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Limita.Infrastructure.Messaging
{
    /// <summary>
    /// Sends SMS through the Twilio REST API (no SDK, just HTTPS). The message text contains the one-time code,
    /// so it is never written to the log, and failures only record the HTTP status and Twilio's error code.
    /// </summary>
    internal sealed class TwilioSmsSender(
        HttpClient http,
        IOptions<SmsOptions> options,
        ILogger<TwilioSmsSender> logger) : ISmsSender
    {
        public async Task SendAsync(string phoneNumber, string message, CancellationToken cancellationToken = default)
        {
            var twilio = options.Value.Twilio;

            if (string.IsNullOrWhiteSpace(twilio.AccountSid) || string.IsNullOrWhiteSpace(twilio.AuthToken))
                throw new InvalidOperationException("Sms:Twilio:AccountSid and Sms:Twilio:AuthToken must be configured.");

            if (string.IsNullOrWhiteSpace(twilio.MessagingServiceSid) && string.IsNullOrWhiteSpace(twilio.From))
                throw new InvalidOperationException(
                    "Set Sms:Twilio:From (a number or registered sender ID) or Sms:Twilio:MessagingServiceSid.");

            var form = new Dictionary<string, string>
            {
                ["To"] = phoneNumber,
                ["Body"] = message
            };

            if (!string.IsNullOrWhiteSpace(twilio.MessagingServiceSid))
                form["MessagingServiceSid"] = twilio.MessagingServiceSid;
            else
                form["From"] = twilio.From;

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"2010-04-01/Accounts/{Uri.EscapeDataString(twilio.AccountSid)}/Messages.json")
            {
                Content = new FormUrlEncodedContent(form)
            };

            request.Headers.Authorization = new AuthenticationHeaderValue(
                "Basic",
                Convert.ToBase64String(Encoding.ASCII.GetBytes($"{twilio.AccountSid}:{twilio.AuthToken}")));

            HttpResponseMessage response;
            try
            {
                response = await http.SendAsync(request, cancellationToken);
            }
            catch (Exception ex) when (ex is HttpRequestException
                                       || (ex is TaskCanceledException && !cancellationToken.IsCancellationRequested))
            {
                logger.LogError(ex, "Could not reach Twilio.");
                throw new SmsDeliveryException("The SMS provider could not be reached.", ex);
            }

            using (response)
            {
                if (response.IsSuccessStatusCode)
                    return;

                var errorCode = await ReadErrorCodeAsync(response, cancellationToken);

                logger.LogError(
                    "Twilio rejected an SMS with HTTP {StatusCode} and Twilio error code {TwilioErrorCode}.",
                    (int)response.StatusCode,
                    errorCode);

                throw new SmsDeliveryException(
                    $"The SMS provider rejected the message (HTTP {(int)response.StatusCode}, Twilio error {errorCode}).");
            }
        }

        private static async Task<string> ReadErrorCodeAsync(HttpResponseMessage response, CancellationToken cancellationToken)
        {
            try
            {
                await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                using var json = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

                return json.RootElement.TryGetProperty("code", out var code) ? code.ToString() : "unknown";
            }
            catch (JsonException)
            {
                return "unknown";
            }



        }
    }
}
