using System;

namespace Sepehr.Contracts
{
    public static class ApiRoutes
    {
        public const string Base = "/api/v1";
        public const string Login = Base + "/auth/login";
        public const string ChangePassword = Base + "/auth/change-password";
        public const string Logout = Base + "/auth/logout";
        public const string Me = Base + "/auth/me";
        public const string Sessions = Base + "/auth/sessions";
        public const string Sync = Base + "/sync";
    }

    public static class HubContract
    {
        public const string Path = "/hubs/chat";
        public const string MessageCreated = "MessageCreated";
        public const string MessageUpdated = "MessageUpdated";
        public const string MessageDeleted = "MessageDeleted";
        public const string ReceiptUpdated = "ReceiptUpdated";
        public const string MembershipChanged = "MembershipChanged";
        public const string Notification = "Notification";
        public const string Presence = "Presence";
    }

    public sealed class ApiError
    {
        public string Code { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string CorrelationId { get; set; } = string.Empty;
    }

    public sealed class LoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string? DeviceInfo { get; set; }
    }

    public sealed class LoginResponse
    {
        public string Token { get; set; } = string.Empty;
        public DateTime AbsoluteExpiresAtUtc { get; set; }
        public bool MustChangePassword { get; set; }
    }

    public sealed class ChangePasswordRequest
    {
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }

    public sealed class MeResponse
    {
        public long UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public bool MustChangePassword { get; set; }
        public string[] GlobalRoles { get; set; } = new string[0];
    }

    public sealed class SessionInfo
    {
        public Guid SessionId { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime LastActivityAtUtc { get; set; }
        public DateTime AbsoluteExpiresAtUtc { get; set; }
        public string? DeviceInfo { get; set; }
        public bool IsCurrent { get; set; }
    }

    public static class ErrorCodes
    {
        public const string InvalidRequest = "InvalidRequest";
        public const string InvalidCredentials = "InvalidCredentials";
        public const string AccountLocked = "AccountLocked";
        public const string TooManyRequests = "TooManyRequests";
        public const string Unauthorized = "Unauthorized";
        public const string Forbidden = "Forbidden";
        public const string PasswordChangeRequired = "PasswordChangeRequired";
        public const string CurrentPasswordIncorrect = "CurrentPasswordIncorrect";
        public const string PasswordTooShort = "PasswordTooShort";
        public const string PasswordTooLong = "PasswordTooLong";
        public const string PasswordSameAsPersonnelCode = "PasswordSameAsPersonnelCode";
        public const string PasswordSameAsCurrent = "PasswordSameAsCurrent";
        public const string HttpsRequired = "HttpsRequired";
        public const string InternalError = "InternalError";
    }

    public sealed class SendMessageRequest
    {
        public Guid ClientMessageId { get; set; }
        public MessageType MessageType { get; set; }
        public string? Text { get; set; }
        public long? ReplyToMessageId { get; set; }
        public Guid[] FileIds { get; set; } = new Guid[0];
    }
}
