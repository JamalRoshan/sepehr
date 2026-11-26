using System;
using System.Security.Claims;
using System.Threading.RateLimiting;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sepehr.Contracts;

namespace Sepehr.Api
{
    /// <summary>محدودیت بر اساس IP، حساب و عملیات (در حافظه همین سرور).</summary>
    public static class RateLimiting
    {
        public const string LoginPolicy = "login";
        public const string PasswordPolicy = "password";

        public static IServiceCollection AddSepehrRateLimiting(
            this IServiceCollection services, IConfiguration configuration)
        {
            var login = Limit(configuration, "RateLimit:LoginPerMinute", 30);
            var password = Limit(configuration, "RateLimit:PasswordChangePerMinute", 10);
            var requests = Limit(configuration, "RateLimit:RequestsPerMinute", 300);

            return services.AddRateLimiter(options =>
            {
                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
                    context => Window(AccountOrAddress(context), requests));
                options.AddPolicy(LoginPolicy, context => Window("login:" + Address(context), login));
                options.AddPolicy(PasswordPolicy,
                    context => Window("password:" + AccountOrAddress(context), password));
                options.OnRejected = (rejected, _) =>
                {
                    var delay = rejected.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter)
                        ? retryAfter
                        : TimeSpan.FromMinutes(1);
                    ApiResponses.SetRetryAfter(rejected.HttpContext, delay);
                    return new ValueTask(ApiResponses.WriteErrorAsync(rejected.HttpContext,
                        StatusCodes.Status429TooManyRequests, ErrorCodes.TooManyRequests,
                        "درخواست‌ها زیاد است؛ کمی بعد دوباره تلاش کنید."));
                };
            });
        }

        private static int Limit(IConfiguration configuration, string key, int fallback)
        {
            var value = configuration.GetValue(key, fallback);
            return value > 0 ? value : fallback;
        }

        private static string Address(HttpContext context) =>
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        private static string AccountOrAddress(HttpContext context)
        {
            var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            return userId != null ? "user:" + userId : "address:" + Address(context);
        }

        private static RateLimitPartition<string> Window(string key, int permits) =>
            RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permits,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            });
    }
}
