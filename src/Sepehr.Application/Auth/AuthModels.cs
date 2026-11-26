using System;
using System.Collections.Generic;
using Sepehr.Domain;

namespace Sepehr.Application.Auth
{
    public sealed record UserAccount(
        long UserId, string Username, string PersonnelCode, string PasswordHash,
        bool IsActive, bool MustChangePassword);

    public sealed record SessionRecord(
        Guid SessionId, long UserId, string Username, string DisplayName,
        bool UserIsActive, bool MustChangePassword,
        DateTime CreatedAtUtc, DateTime ExpiresAtUtc, DateTime LastActivityAtUtc, DateTime? RevokedAtUtc);

    public sealed record NewSession(
        Guid SessionId, long UserId, byte[] TokenHash,
        DateTime CreatedAtUtc, DateTime ExpiresAtUtc, string? DeviceInfo);

    public sealed record SessionSummary(
        Guid SessionId, DateTime CreatedAtUtc, DateTime LastActivityAtUtc, DateTime ExpiresAtUtc, string? DeviceInfo);

    public sealed record SecurityOptions(SessionOptions Session, LockoutOptions Lockout);

    /// <summary>اطلاعات درخواست برای ثبت رویداد؛ هرگز شامل رمز یا توکن نیست.</summary>
    public sealed record RequestContext(string? IpAddress, Guid CorrelationId);

    public static class AttemptReasons
    {
        /* فقط این دو در شمارش قفل حساب‌اند */
        public const string BadCredentials = "BadCredentials";
        public const string BadCurrentPassword = "BadCurrentPassword";

        public const string Locked = "Locked";
        public const string Inactive = "Inactive";
    }

    public sealed record LoginAttempt(
        string Username, long? UserId, DateTime AtUtc, string? IpAddress, bool Success, string? FailureReason);

    public static class AuditActions
    {
        public const string LoginSucceeded = "Auth.LoginSucceeded";
        public const string LoginFailed = "Auth.LoginFailed";
        public const string LoginBlocked = "Auth.LoginBlocked";
        public const string PasswordChanged = "Auth.PasswordChanged";
        public const string PasswordChangeFailed = "Auth.PasswordChangeFailed";
        public const string Logout = "Auth.Logout";
    }

    public sealed record AuditEntry(
        long? ActorUserId, string Action, long? TargetUserId, DateTime AtUtc, Guid CorrelationId, bool Success);

    public enum LoginStatus { Success, InvalidCredentials, Locked, Busy }

    public sealed record LoginResult(
        LoginStatus Status, string? Token = null, DateTime? ExpiresAtUtc = null,
        bool MustChangePassword = false, DateTime? LockedUntilUtc = null);

    public enum ChangePasswordStatus { Success, CurrentPasswordIncorrect, InvalidNewPassword, Locked, Busy, SessionInvalid }

    public sealed record ChangePasswordResult(
        ChangePasswordStatus Status, PasswordError PasswordError = PasswordError.None, DateTime? LockedUntilUtc = null);

    public sealed record CurrentUser(
        long UserId, string Username, string DisplayName, bool MustChangePassword, IReadOnlyList<string> GlobalRoles);
}
