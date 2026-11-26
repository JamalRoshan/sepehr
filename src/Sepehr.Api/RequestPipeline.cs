using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Sepehr.Contracts;

namespace Sepehr.Api
{
    /// <summary>شناسه پیگیری، سرآیندهای امنیتی و پاسخ خطای یکسان. بدنه درخواست هرگز Log نمی‌شود.</summary>
    public sealed class RequestSafetyMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestSafetyMiddleware> _logger;

        public RequestSafetyMiddleware(RequestDelegate next, ILogger<RequestSafetyMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var correlationId = Guid.NewGuid();
            ApiResponses.SetCorrelationId(context, correlationId);

            var headers = context.Response.Headers;
            headers[ApiResponses.CorrelationHeader] = correlationId.ToString();
            headers.CacheControl = "no-store";
            headers.XContentTypeOptions = "nosniff";

            try
            {
                await _next(context);
            }
            catch (BadHttpRequestException exception) when (!context.Response.HasStarted)
            {
                await ApiResponses.WriteErrorAsync(context, exception.StatusCode,
                    ErrorCodes.InvalidRequest, "درخواست نامعتبر است.");
            }
            catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
            {
                /* کاربر اتصال را قطع کرده است. */
            }
            catch (Exception exception) when (!context.Response.HasStarted)
            {
                _logger.LogError(exception, "Unhandled error. CorrelationId={CorrelationId}", correlationId);
                await ApiResponses.WriteErrorAsync(context, StatusCodes.Status500InternalServerError,
                    ErrorCodes.InternalError, "خطای داخلی سرور رخ داد.");
            }
        }
    }

    /// <summary>
    /// درخواست HTTP رد می‌شود و Redirect نمی‌شود، تا کلاینت اشتباه پیکربندی‌شده زود آشکار شود.
    /// </summary>
    public sealed class HttpsOnlyMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly bool _allowInsecure;

        public HttpsOnlyMiddleware(RequestDelegate next, IConfiguration configuration)
        {
            _next = next;
            _allowInsecure = configuration.GetValue("Security:AllowInsecureHttp", false);
        }

        public Task InvokeAsync(HttpContext context)
        {
            if (!context.Request.IsHttps && !_allowInsecure)
                return ApiResponses.WriteErrorAsync(context, StatusCodes.Status400BadRequest,
                    ErrorCodes.HttpsRequired, "ارتباط فقط از طریق HTTPS مجاز است.");

            if (context.Request.IsHttps)
                context.Response.Headers.StrictTransportSecurity = "max-age=31536000";

            return _next(context);
        }
    }
}
