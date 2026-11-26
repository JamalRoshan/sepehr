using System;
using System.Globalization;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sepehr.Application.Auth;
using Sepehr.Contracts;

namespace Sepehr.Api
{
    public static class SessionAuthenticationDefaults
    {
        public const string Scheme = "SepehrSession";
        public const string PasswordStateClaim = "sepehr:password";
        public const string PasswordCurrent = "current";
        public const string PasswordPending = "pending";

        /// <summary>فقط برای مسیرهایی که پیش از تغییر رمز اولیه هم مجازند.</summary>
        public const string AllowPendingPasswordPolicy = "AllowPendingPassword";

        private const string SessionKey = "Sepehr.Session";

        public static void SetSession(HttpContext context, SessionRecord session) => context.Items[SessionKey] = session;

        public static SessionRecord Session(HttpContext context) =>
            context.Items[SessionKey] as SessionRecord
            ?? throw new InvalidOperationException("No authenticated session.");
    }

    public sealed class SessionAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        private const string Prefix = "Bearer ";
        private const int MaximumTokenLength = 128;
        private readonly AuthService _auth;

        public SessionAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder,
            AuthService auth)
            : base(options, logger, encoder)
        {
            _auth = auth;
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var header = Request.Headers.Authorization.ToString();
            if (!header.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase)) return AuthenticateResult.NoResult();

            var token = header.Substring(Prefix.Length).Trim();
            if (token.Length == 0 || token.Length > MaximumTokenLength)
                return AuthenticateResult.Fail("Invalid token.");

            var session = await _auth.AuthenticateAsync(token, Context.RequestAborted);
            if (session == null) return AuthenticateResult.Fail("Invalid session.");

            SessionAuthenticationDefaults.SetSession(Context, session);
            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, session.UserId.ToString(CultureInfo.InvariantCulture)),
                new Claim(SessionAuthenticationDefaults.PasswordStateClaim,
                    session.MustChangePassword
                        ? SessionAuthenticationDefaults.PasswordPending
                        : SessionAuthenticationDefaults.PasswordCurrent)
            }, SessionAuthenticationDefaults.Scheme);

            return AuthenticateResult.Success(new AuthenticationTicket(
                new ClaimsPrincipal(identity), SessionAuthenticationDefaults.Scheme));
        }

        protected override Task HandleChallengeAsync(AuthenticationProperties properties) =>
            ApiResponses.WriteErrorAsync(Context, StatusCodes.Status401Unauthorized,
                ErrorCodes.Unauthorized, "ابتدا وارد سامانه شوید.");

        protected override Task HandleForbiddenAsync(AuthenticationProperties properties) =>
            Context.User.HasClaim(SessionAuthenticationDefaults.PasswordStateClaim,
                SessionAuthenticationDefaults.PasswordPending)
                ? ApiResponses.WriteErrorAsync(Context, StatusCodes.Status403Forbidden,
                    ErrorCodes.PasswordChangeRequired, "ابتدا باید رمز عبور خود را تغییر دهید.")
                : ApiResponses.WriteErrorAsync(Context, StatusCodes.Status403Forbidden,
                    ErrorCodes.Forbidden, "اجازه انجام این کار را ندارید.");
    }
}
