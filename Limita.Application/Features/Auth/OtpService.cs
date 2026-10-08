using Limita.Application.Common;
using Limita.Application.Common.Abstractions;
using Limita.Domain.Entities;
using Limita.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Limita.Application.Features.Auth
{
    internal sealed class OtpService(
        IApplicationDbContext db,
        ISecretGenerator generator,
        ISecretHasher hasher,
        ISmsSender sms,
        TimeProvider time)
    {
        public async Task<Result> IssueAsync(User user, OtpPurpose purpose, CancellationToken cancellationToken)
        {
            var now = time.GetUtcNow();

            var latest = await db.OtpCodes
                .Where(o => o.UserId == user.Id && o.Purpose == purpose)
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (latest is not null && now - latest.CreatedAt < AuthSettings.OtpResendCooldown)
                return Result.Failure(AuthErrors.OtpCooldown);

            // Only the newest code may be used.
            var open = await db.OtpCodes
                .Where(o => o.UserId == user.Id && o.Purpose == purpose && o.ConsumedAt == null)
                .ToListAsync(cancellationToken);

            foreach (var code in open)
                code.Invalidate(now);

            var plainCode = generator.NewOtpCode();

            db.OtpCodes.Add(OtpCode.Create(
                user.Id,
                purpose,
                hasher.Hash(Payload(user.Id, purpose, plainCode)),
                now.Add(AuthSettings.OtpLifetime),
                now));

            var minutes = (int)AuthSettings.OtpLifetime.TotalMinutes;
            var message = purpose == OtpPurpose.PasswordReset
                ? $"Your Limita password reset code is {plainCode}. It expires in {minutes} minutes. Never share it."
                : $"Your Limita verification code is {plainCode}. It expires in {minutes} minutes. Never share it.";

            await sms.SendAsync(user.PhoneNumber, message, cancellationToken);

            return Result.Success();

        }

        /// <summary>
        /// Checks a code. A wrong guess is counted, so commands that call this must implement ICommitOnFailure
        /// to keep the counter. Every failure returns the same error so attackers learn nothing.
        /// </summary>
        public async Task<Result> VerifyAsync(User user, OtpPurpose purpose, string code, CancellationToken cancellationToken)
        {
            var now = time.GetUtcNow();

            var otp = await db.OtpCodes
                .Where(o => o.UserId == user.Id && o.Purpose == purpose && o.ConsumedAt == null)
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (otp is null || !otp.IsValid(now))
                return Result.Failure(AuthErrors.InvalidCode);

            if (!hasher.Verify(Payload(user.Id, purpose, code), otp.CodeHash))
            {
                otp.RegisterFailedAttempt();
                return Result.Failure(AuthErrors.InvalidCode);
            }

            otp.Consume(now);
            return Result.Success();
        }

        // The hash is bound to the user and purpose, so a code cannot be replayed for something else.
        private static string Payload(Guid userId, OtpPurpose purpose, string code) => $"{userId:N}:{purpose}:{code}";
    }
}
