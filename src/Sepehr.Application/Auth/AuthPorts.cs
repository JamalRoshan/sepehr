using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Sepehr.Application.Auth
{
    public interface IPasswordHasher
    {
        string Hash(string password);
        bool Verify(string password, string encodedHash);
    }

    public interface ISessionTokens
    {
        (string Token, byte[] TokenHash) Create();
        byte[] ComputeHash(string token);
    }

    public interface IAuthStore
    {
        Task<SecurityOptions> GetSecurityOptionsAsync(CancellationToken cancellationToken);

        /// <summary>
        /// تراکنشی با قفل انحصاری روی حساب باز می‌کند تا شمارش شکست‌ها و ثبت تلاش جدید اتمی باشد.
        /// اگر قفل در زمان مجاز گرفته نشود null برمی‌گردد.
        /// </summary>
        Task<IAccountScope?> LockAccountAsync(string username, CancellationToken cancellationToken);

        Task<SessionRecord?> FindSessionAsync(byte[] tokenHash, CancellationToken cancellationToken);
        Task TouchSessionAsync(Guid sessionId, DateTime nowUtc, CancellationToken cancellationToken);
        Task<IReadOnlyList<string>> GetGlobalRolesAsync(long userId, CancellationToken cancellationToken);

        Task<IReadOnlyList<SessionSummary>> GetOpenSessionsAsync(
            long userId, DateTime nowUtc, TimeSpan idleTimeout, CancellationToken cancellationToken);

        /// <summary>ابطال و Audit در یک تراکنش.</summary>
        Task RevokeSessionAsync(Guid sessionId, AuditEntry audit, CancellationToken cancellationToken);
    }

    /// <summary>بدون Commit، Dispose همه تغییرات را برمی‌گرداند.</summary>
    public interface IAccountScope : IAsyncDisposable
    {
        UserAccount? Account { get; }

        /// <summary>شکست‌های شمارش‌شونده پس از آخرین ورود موفق و از زمان داده‌شده.</summary>
        Task<IReadOnlyList<DateTime>> GetRecentFailuresAsync(DateTime sinceUtc, CancellationToken cancellationToken);

        Task AddAttemptAsync(LoginAttempt attempt, CancellationToken cancellationToken);
        Task AddSessionAsync(NewSession session, CancellationToken cancellationToken);
        Task<bool> IsSessionOpenAsync(Guid sessionId, CancellationToken cancellationToken);

        Task SetPasswordAsync(string passwordHash, DateTime nowUtc, CancellationToken cancellationToken);

        Task RevokeOtherSessionsAsync(Guid keepSessionId, DateTime nowUtc, CancellationToken cancellationToken);
        Task AddAuditAsync(AuditEntry entry, CancellationToken cancellationToken);
        Task CommitAsync(CancellationToken cancellationToken);
    }
}
