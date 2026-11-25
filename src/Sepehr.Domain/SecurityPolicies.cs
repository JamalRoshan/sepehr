using System;
using System.Collections.Generic;
using System.Linq;

namespace Sepehr.Domain
{
    public enum PasswordError { None, TooShort, TooLong, SameAsPersonnelCode, SameAsCurrent }

    /// <summary>رمز هرگز Trim یا نرمال‌سازی نمی‌شود.</summary>
    public static class PasswordPolicy
    {
        public const int MinimumLength = 8;
        public const int MaximumLength = 128;

        public static PasswordError ValidateNew(string newPassword, string currentPassword, string personnelCode)
        {
            if (newPassword == null) throw new ArgumentNullException(nameof(newPassword));
            if (newPassword.Length < MinimumLength) return PasswordError.TooShort;
            if (newPassword.Length > MaximumLength) return PasswordError.TooLong;
            if (string.Equals(newPassword, personnelCode, StringComparison.Ordinal))
                return PasswordError.SameAsPersonnelCode;
            if (string.Equals(newPassword, currentPassword, StringComparison.Ordinal))
                return PasswordError.SameAsCurrent;
            return PasswordError.None;
        }
    }

    public sealed record LockoutOptions(int MaxFailures, TimeSpan Window, TimeSpan Duration)
    {
        public static LockoutOptions Default { get; } =
            new(5, TimeSpan.FromMinutes(15), TimeSpan.FromMinutes(15));
    }

    public static class LockoutPolicy
    {
        /// <summary>
        /// تلاش‌های هنگام قفل نباید به‌عنوان شکست جدید رمز ثبت شوند.
        /// ورودی فقط شکست‌های پس از آخرین ورود موفق است.
        /// </summary>
        public static DateTime? LockedUntil(IEnumerable<DateTime> failuresUtc, DateTime nowUtc, LockoutOptions options)
        {
            var ordered = failuresUtc.OrderBy(time => time).ToList();
            DateTime? lockedUntil = null;

            for (var last = options.MaxFailures - 1; last < ordered.Count; last++)
            {
                var first = last - options.MaxFailures + 1;
                if (ordered[last] - ordered[first] <= options.Window)
                    lockedUntil = ordered[last] + options.Duration;
            }

            return lockedUntil > nowUtc ? lockedUntil : null;
        }
    }

    public sealed record SessionOptions(TimeSpan IdleTimeout, TimeSpan AbsoluteLifetime)
    {
        public static SessionOptions Default { get; } =
            new(TimeSpan.FromMinutes(30), TimeSpan.FromHours(12));
    }

    public static class SessionPolicy
    {
        public static bool IsActive(
            DateTime createdAtUtc, DateTime lastActivityAtUtc, DateTime? revokedAtUtc,
            DateTime nowUtc, SessionOptions options) =>
            revokedAtUtc == null
            && nowUtc < createdAtUtc + options.AbsoluteLifetime
            && nowUtc < lastActivityAtUtc + options.IdleTimeout;
    }
}
