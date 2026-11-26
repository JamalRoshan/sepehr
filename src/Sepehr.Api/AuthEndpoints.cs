using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Sepehr.Application.Auth;
using Sepehr.Contracts;
using Sepehr.Domain;

namespace Sepehr.Api
{
    public static class AuthEndpoints
    {
        public static void MapAuthEndpoints(this IEndpointRouteBuilder routes)
        {
            routes.MapPost(ApiRoutes.Login, LoginAsync)
                .AllowAnonymous()
                .RequireRateLimiting(RateLimiting.LoginPolicy);

            routes.MapPost(ApiRoutes.ChangePassword, ChangePasswordAsync)
                .RequireAuthorization(SessionAuthenticationDefaults.AllowPendingPasswordPolicy)
                .RequireRateLimiting(RateLimiting.PasswordPolicy);

            routes.MapPost(ApiRoutes.Logout, LogoutAsync)
                .RequireAuthorization(SessionAuthenticationDefaults.AllowPendingPasswordPolicy);

            routes.MapGet(ApiRoutes.Me, MeAsync)
                .RequireAuthorization(SessionAuthenticationDefaults.AllowPendingPasswordPolicy);

            /* بدون Policy نام‌دار: سیاست پیش‌فرض، تغییر رمز اولیه را الزامی می‌کند. */
            routes.MapGet(ApiRoutes.Sessions, SessionsAsync).RequireAuthorization();
        }

        private static RequestContext ContextOf(HttpContext http) =>
            new(http.Connection.RemoteIpAddress?.ToString(), ApiResponses.CorrelationId(http));

        private static async Task<IResult> LoginAsync(
            LoginRequest request, HttpContext http, AuthService auth, TimeProvider time,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Username)
                || request.Username.Length > AuthService.MaximumUsernameLength
                || string.IsNullOrEmpty(request.Password)
                || request.Password.Length > PasswordPolicy.MaximumLength)
                return ApiResponses.Error(http, StatusCodes.Status400BadRequest,
                    ErrorCodes.InvalidRequest, "نام کاربری و رمز عبور معتبر وارد کنید.");

            var result = await auth.LoginAsync(
                request.Username, request.Password, request.DeviceInfo, ContextOf(http), cancellationToken);

            switch (result.Status)
            {
                case LoginStatus.Success:
                    return Results.Ok(new LoginResponse
                    {
                        Token = result.Token!,
                        AbsoluteExpiresAtUtc = result.ExpiresAtUtc!.Value,
                        MustChangePassword = result.MustChangePassword
                    });
                case LoginStatus.Locked:
                    return Locked(http, result.LockedUntilUtc!.Value, time);
                case LoginStatus.Busy:
                    return Busy(http);
                default:
                    return ApiResponses.Error(http, StatusCodes.Status401Unauthorized,
                        ErrorCodes.InvalidCredentials, "نام کاربری یا رمز عبور نادرست است.");
            }
        }

        private static async Task<IResult> ChangePasswordAsync(
            ChangePasswordRequest request, HttpContext http, AuthService auth, TimeProvider time,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(request.CurrentPassword)
                || request.CurrentPassword.Length > PasswordPolicy.MaximumLength
                || request.NewPassword == null)
                return ApiResponses.Error(http, StatusCodes.Status400BadRequest,
                    ErrorCodes.InvalidRequest, "رمز فعلی و رمز جدید را وارد کنید.");

            var session = SessionAuthenticationDefaults.Session(http);
            var result = await auth.ChangePasswordAsync(session.Username, session.SessionId,
                request.CurrentPassword, request.NewPassword, ContextOf(http), cancellationToken);

            switch (result.Status)
            {
                case ChangePasswordStatus.Success:
                    return Results.NoContent();
                case ChangePasswordStatus.CurrentPasswordIncorrect:
                    return ApiResponses.Error(http, StatusCodes.Status400BadRequest,
                        ErrorCodes.CurrentPasswordIncorrect, "رمز فعلی نادرست است.");
                case ChangePasswordStatus.InvalidNewPassword:
                    var (code, message) = Describe(result.PasswordError);
                    return ApiResponses.Error(http, StatusCodes.Status400BadRequest, code, message);
                case ChangePasswordStatus.Locked:
                    return Locked(http, result.LockedUntilUtc!.Value, time);
                case ChangePasswordStatus.Busy:
                    return Busy(http);
                default:
                    return ApiResponses.Error(http, StatusCodes.Status401Unauthorized,
                        ErrorCodes.Unauthorized, "ابتدا وارد سامانه شوید.");
            }
        }

        private static async Task<IResult> LogoutAsync(
            HttpContext http, AuthService auth, CancellationToken cancellationToken)
        {
            var session = SessionAuthenticationDefaults.Session(http);
            await auth.LogoutAsync(session.UserId, session.SessionId, ContextOf(http), cancellationToken);
            return Results.NoContent();
        }

        private static async Task<IResult> MeAsync(
            HttpContext http, AuthService auth, CancellationToken cancellationToken)
        {
            var user = await auth.GetCurrentUserAsync(SessionAuthenticationDefaults.Session(http), cancellationToken);
            return Results.Ok(new MeResponse
            {
                UserId = user.UserId,
                Username = user.Username,
                DisplayName = user.DisplayName,
                MustChangePassword = user.MustChangePassword,
                GlobalRoles = user.GlobalRoles.ToArray()
            });
        }

        private static async Task<IResult> SessionsAsync(
            HttpContext http, AuthService auth, CancellationToken cancellationToken)
        {
            var current = SessionAuthenticationDefaults.Session(http);
            var sessions = await auth.GetOpenSessionsAsync(current.UserId, cancellationToken);
            return Results.Ok(sessions.Select(session => new SessionInfo
            {
                SessionId = session.SessionId,
                CreatedAtUtc = session.CreatedAtUtc,
                LastActivityAtUtc = session.LastActivityAtUtc,
                AbsoluteExpiresAtUtc = session.ExpiresAtUtc,
                DeviceInfo = session.DeviceInfo,
                IsCurrent = session.SessionId == current.SessionId
            }).ToArray());
        }

        private static IResult Locked(HttpContext http, DateTime lockedUntilUtc, TimeProvider time)
        {
            ApiResponses.SetRetryAfter(http, lockedUntilUtc - time.GetUtcNow().UtcDateTime);
            return ApiResponses.Error(http, StatusCodes.Status429TooManyRequests,
                ErrorCodes.AccountLocked, "به دلیل تلاش‌های ناموفق، ورود موقتاً قفل شده است.");
        }

        private static IResult Busy(HttpContext http)
        {
            ApiResponses.SetRetryAfter(http, TimeSpan.FromSeconds(5));
            return ApiResponses.Error(http, StatusCodes.Status429TooManyRequests,
                ErrorCodes.TooManyRequests, "درخواست‌ها زیاد است؛ کمی بعد دوباره تلاش کنید.");
        }

        private static (string Code, string Message) Describe(PasswordError error) => error switch
        {
            PasswordError.TooShort => (ErrorCodes.PasswordTooShort,
                "رمز جدید باید دست‌کم 8 نویسه باشد."),
            PasswordError.TooLong => (ErrorCodes.PasswordTooLong, "رمز جدید بیش از اندازه بلند است."),
            PasswordError.SameAsPersonnelCode => (ErrorCodes.PasswordSameAsPersonnelCode,
                "رمز جدید نباید با کد پرسنلی یکسان باشد."),
            PasswordError.SameAsCurrent => (ErrorCodes.PasswordSameAsCurrent,
                "رمز جدید نباید با رمز فعلی یکسان باشد."),
            _ => throw new ArgumentOutOfRangeException(nameof(error))
        };
    }
}
