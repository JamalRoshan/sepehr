using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Sepehr.Contracts;

namespace Sepehr.Api
{
    public static class ApiResponses
    {
        public const string CorrelationHeader = "X-Correlation-ID";
        private const string CorrelationKey = "Sepehr.CorrelationId";

        public static void SetCorrelationId(HttpContext context, Guid id) => context.Items[CorrelationKey] = id;

        public static Guid CorrelationId(HttpContext context) =>
            context.Items.TryGetValue(CorrelationKey, out var value) && value is Guid id ? id : Guid.Empty;

        private static ApiError Create(HttpContext context, string code, string message) => new()
        {
            Code = code,
            Message = message,
            CorrelationId = CorrelationId(context).ToString()
        };

        public static IResult Error(HttpContext context, int status, string code, string message) =>
            Results.Json(Create(context, code, message), statusCode: status);

        public static Task WriteErrorAsync(HttpContext context, int status, string code, string message)
        {
            context.Response.StatusCode = status;
            return context.Response.WriteAsJsonAsync(Create(context, code, message));
        }

        public static void SetRetryAfter(HttpContext context, TimeSpan delay) =>
            context.Response.Headers.RetryAfter =
                Math.Max(1, (int)Math.Ceiling(delay.TotalSeconds)).ToString(CultureInfo.InvariantCulture);
    }
}
