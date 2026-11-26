using System;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sepehr.Api;
using Sepehr.Application.Auth;
using Sepehr.Infrastructure.Data;
using Sepehr.Infrastructure.Security;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.AddServerHeader = false;
    /* آپلود فایل در Endpoint خودش سقف جداگانه می‌گیرد. */
    options.Limits.MaxRequestBodySize = 64 * 1024;
});

var connectionString = builder.Configuration.GetConnectionString("Sepehr")
    ?? throw new InvalidOperationException("Connection string 'Sepehr' is not configured.");

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IAuthStore>(_ => new SqlAuthStore(connectionString));
builder.Services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
builder.Services.AddSingleton<ISessionTokens, OpaqueSessionTokens>();
builder.Services.AddSingleton<DummyPasswordHash>();
builder.Services.AddScoped<AuthService>();

builder.Services.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = true);

builder.Services
    .AddAuthentication(SessionAuthenticationDefaults.Scheme)
    .AddScheme<AuthenticationSchemeOptions, SessionAuthenticationHandler>(
        SessionAuthenticationDefaults.Scheme, null);

builder.Services.AddAuthorization(options =>
{
    var fullAccess = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .RequireClaim(SessionAuthenticationDefaults.PasswordStateClaim,
            SessionAuthenticationDefaults.PasswordCurrent)
        .Build();

    options.DefaultPolicy = fullAccess;
    options.FallbackPolicy = fullAccess; /* پیش‌فرض: ممنوع */
    options.AddPolicy(SessionAuthenticationDefaults.AllowPendingPasswordPolicy,
        policy => policy.RequireAuthenticatedUser());
});

builder.Services.AddSepehrRateLimiting(builder.Configuration);

var app = builder.Build();

app.UseMiddleware<RequestSafetyMiddleware>();
app.UseMiddleware<HttpsOnlyMiddleware>();
app.UseRouting();
app.UseAuthentication();
app.UseRateLimiter(); /* پس از احراز هویت تا سهمیه هر حساب جدا باشد */
app.UseAuthorization();
app.MapAuthEndpoints();

app.Run();

public partial class Program
{
}
