using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Sepehr.Domain;

namespace Sepehr.Application.Auth
{
    public sealed class AuthService
    {
        public const int MaximumUsernameLength = 32;
        private const int MaximumDeviceInfoLength = 256;

        /* برای کاهش نوشتن، آخرین فعالیت حداکثر هر 30 ثانیه ثبت می‌شود. */
        private static readonly TimeSpan TouchInterval = TimeSpan.FromSeconds(30);

        private readonly IAuthStore _store;
        private readonly IPasswordHasher _hasher;
        private readonly ISessionTokens _tokens;
        private readonly TimeProvider _time;
        private readonly DummyPasswordHash _dummy;

        public AuthService(
            IAuthStore store, IPasswordHasher hasher, ISessionTokens tokens, TimeProvider time, DummyPasswordHash dummy)
        {
            _store = store;
            _hasher = hasher;
            _tokens = tokens;
            _time = time;
            _dummy = dummy;
        }

        private DateTime UtcNow => _time.GetUtcNow().UtcDateTime;

        /// <summary>ورودی‌ها باید پیش‌تر از نظر خالی‌نبودن و طول بررسی شده باشند.</summary>
        public async Task<LoginResult> LoginAsync(
            string username, string password, string? deviceInfo, RequestContext context,
            CancellationToken cancellationToken)
        {
            var options = await _store.GetSecurityOptionsAsync(cancellationToken);
            await using var scope = await _store.LockAccountAsync(username, cancellationToken);
            if (scope == null) return new LoginResult(LoginStatus.Busy);

            var account = scope.Account;
            var now = UtcNow;

            var lockedUntil = await LockedUntilAsync(scope, options.Lockout, now, cancellationToken);
            if (lockedUntil != null)
            {
                await RecordFailureAsync(scope, username, account, now, context,
                    AttemptReasons.Locked, AuditActions.LoginBlocked, null, cancellationToken);
                return new LoginResult(LoginStatus.Locked, LockedUntilUtc: lockedUntil);
            }

            /* برای کاربر ناموجود هم Hash محاسبه می‌شود تا زمان پاسخ وجود حساب را لو ندهد. */
            var passwordMatches = _hasher.Verify(password, account?.PasswordHash ?? _dummy.Value);

            if (account == null || !passwordMatches || !account.IsActive)
            {
                var reason = account != null && passwordMatches
                    ? AttemptReasons.Inactive
                    : AttemptReasons.BadCredentials;
                await RecordFailureAsync(scope, username, account, now, context,
                    reason, AuditActions.LoginFailed, null, cancellationToken);
                return new LoginResult(LoginStatus.InvalidCredentials);
            }

            var (token, tokenHash) = _tokens.Create();
            var expiresAt = now + options.Session.AbsoluteLifetime;
            await scope.AddSessionAsync(new NewSession(
                Guid.NewGuid(), account.UserId, tokenHash, now, expiresAt,
                Truncate(deviceInfo, MaximumDeviceInfoLength)), cancellationToken);
            await scope.AddAttemptAsync(new LoginAttempt(
                account.Username, account.UserId, now, context.IpAddress, true, null), cancellationToken);
            await scope.AddAuditAsync(new AuditEntry(
                account.UserId, AuditActions.LoginSucceeded, account.UserId, now, context.CorrelationId, true),
                cancellationToken);
            await scope.CommitAsync(cancellationToken);

            return new LoginResult(LoginStatus.Success, token, expiresAt, account.MustChangePassword);
        }

        /// <summary>نشست معتبر را برمی‌گرداند و آخرین فعالیت را به‌روز می‌کند.</summary>
        public async Task<SessionRecord?> AuthenticateAsync(string token, CancellationToken cancellationToken)
        {
            var session = await _store.FindSessionAsync(_tokens.ComputeHash(token), cancellationToken);
            if (session == null || !session.UserIsActive) return null;

            var options = await _store.GetSecurityOptionsAsync(cancellationToken);
            var now = UtcNow;
            var sessionOptions = new SessionOptions(
                options.Session.IdleTimeout, session.ExpiresAtUtc - session.CreatedAtUtc);

            if (!SessionPolicy.IsActive(
                    session.CreatedAtUtc, session.LastActivityAtUtc, session.RevokedAtUtc, now, sessionOptions))
                return null;

            if (now - session.LastActivityAtUtc >= TouchInterval)
                await _store.TouchSessionAsync(session.SessionId, now, cancellationToken);

            return session;
        }

        public async Task<ChangePasswordResult> ChangePasswordAsync(
            string username, Guid sessionId, string currentPassword, string newPassword,
            RequestContext context, CancellationToken cancellationToken)
        {
            var options = await _store.GetSecurityOptionsAsync(cancellationToken);
            await using var scope = await _store.LockAccountAsync(username, cancellationToken);
            if (scope == null) return new ChangePasswordResult(ChangePasswordStatus.Busy);

            var account = scope.Account;
            if (account == null || !account.IsActive
                || !await scope.IsSessionOpenAsync(sessionId, cancellationToken))
                return new ChangePasswordResult(ChangePasswordStatus.SessionInvalid);

            var now = UtcNow;
            var lockedUntil = await LockedUntilAsync(scope, options.Lockout, now, cancellationToken);
            if (lockedUntil != null)
                return new ChangePasswordResult(ChangePasswordStatus.Locked, LockedUntilUtc: lockedUntil);

            /* رمز فعلی اشتباه هم در قفل حساب شمرده می‌شود تا نشست دزدیده‌شده راه حدس رمز نباشد. */
            if (!_hasher.Verify(currentPassword, account.PasswordHash))
            {
                await RecordFailureAsync(scope, account.Username, account, now, context,
                    AttemptReasons.BadCurrentPassword, AuditActions.PasswordChangeFailed,
                    account.UserId, cancellationToken);
                return new ChangePasswordResult(ChangePasswordStatus.CurrentPasswordIncorrect);
            }

            var error = PasswordPolicy.ValidateNew(newPassword, currentPassword, account.PersonnelCode);
            if (error != PasswordError.None)
                return new ChangePasswordResult(ChangePasswordStatus.InvalidNewPassword, error);

            await scope.SetPasswordAsync(_hasher.Hash(newPassword), now, cancellationToken);
            await scope.RevokeOtherSessionsAsync(sessionId, now, cancellationToken);
            await scope.AddAuditAsync(new AuditEntry(
                account.UserId, AuditActions.PasswordChanged, account.UserId, now, context.CorrelationId, true),
                cancellationToken);
            await scope.CommitAsync(cancellationToken);

            return new ChangePasswordResult(ChangePasswordStatus.Success);
        }

        public Task LogoutAsync(
            long userId, Guid sessionId, RequestContext context, CancellationToken cancellationToken) =>
            _store.RevokeSessionAsync(sessionId,
                new AuditEntry(userId, AuditActions.Logout, userId, UtcNow, context.CorrelationId, true),
                cancellationToken);

        public async Task<CurrentUser> GetCurrentUserAsync(SessionRecord session, CancellationToken cancellationToken) =>
            new(session.UserId, session.Username, session.DisplayName, session.MustChangePassword,
                await _store.GetGlobalRolesAsync(session.UserId, cancellationToken));

        public async Task<IReadOnlyList<SessionSummary>> GetOpenSessionsAsync(
            long userId, CancellationToken cancellationToken)
        {
            var options = await _store.GetSecurityOptionsAsync(cancellationToken);
            return await _store.GetOpenSessionsAsync(userId, UtcNow, options.Session.IdleTimeout, cancellationToken);
        }

        private static async Task<DateTime?> LockedUntilAsync(
            IAccountScope scope, LockoutOptions options, DateTime now, CancellationToken cancellationToken)
        {
            var failures = await scope.GetRecentFailuresAsync(
                now - options.Window - options.Duration, cancellationToken);
            return LockoutPolicy.LockedUntil(failures, now, options);
        }

        private static async Task RecordFailureAsync(
            IAccountScope scope, string username, UserAccount? account, DateTime now, RequestContext context,
            string reason, string auditAction, long? actorUserId, CancellationToken cancellationToken)
        {
            await scope.AddAttemptAsync(new LoginAttempt(
                account?.Username ?? username, account?.UserId, now, context.IpAddress, false, reason),
                cancellationToken);
            /* نام کاربری ناشناخته در Audit ثبت نمی‌شود (ممکن است رمز اشتباه‌تایپ‌شده باشد). */
            await scope.AddAuditAsync(new AuditEntry(
                actorUserId, auditAction, account?.UserId, now, context.CorrelationId, false), cancellationToken);
            await scope.CommitAsync(cancellationToken);
        }

        private static string? Truncate(string? value, int length) =>
            value == null || value.Length <= length ? value : value.Substring(0, length);
    }

    /// <summary>Hash ساختگی که یک‌بار در طول عمر برنامه ساخته می‌شود.</summary>
    public sealed class DummyPasswordHash
    {
        private readonly Lazy<string> _value;

        public DummyPasswordHash(IPasswordHasher hasher) =>
            _value = new Lazy<string>(() => hasher.Hash(Guid.NewGuid().ToString("N")));

        public string Value => _value.Value;
    }
}
