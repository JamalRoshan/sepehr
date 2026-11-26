using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Sepehr.Contracts;
using Xunit;

namespace Sepehr.Tests.Integration
{
    public sealed class TestClock : TimeProvider
    {
        private DateTimeOffset _now = DateTimeOffset.UtcNow;
        public override DateTimeOffset GetUtcNow() => _now;
        public void Advance(TimeSpan by) => _now += by;
    }

    public sealed class TestApi : IDisposable
    {
        private readonly WebApplicationFactory<Program> _factory;

        public TestApi(DatabaseFixture database, int loginPerMinute = 1000)
        {
            _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Production");
                builder.UseSetting("ConnectionStrings:Sepehr", database.ConnectionString);
                builder.UseSetting("RateLimit:LoginPerMinute", loginPerMinute.ToString());
                builder.UseSetting("RateLimit:PasswordChangePerMinute", "1000");
                builder.UseSetting("RateLimit:RequestsPerMinute", "10000");
                builder.ConfigureTestServices(services => services.AddSingleton<TimeProvider>(Clock));
            });
            Client = CreateClient("https://localhost");
        }

        public TestClock Clock { get; } = new();
        public HttpClient Client { get; }

        public HttpClient CreateClient(string baseAddress) => _factory.CreateClient(
            new WebApplicationFactoryClientOptions { BaseAddress = new Uri(baseAddress), AllowAutoRedirect = false });

        public Task<HttpResponseMessage> LoginAsync(string username, string password) =>
            Client.PostAsJsonAsync(ApiRoutes.Login, new LoginRequest { Username = username, Password = password });

        public async Task<LoginResponse> LoginOkAsync(string username, string password)
        {
            var response = await LoginAsync(username, password);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return (await response.Content.ReadFromJsonAsync<LoginResponse>())!;
        }

        public Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, string token, object? body = null)
        {
            var request = new HttpRequestMessage(method, path);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            if (body != null) request.Content = JsonContent.Create(body, body.GetType());
            return Client.SendAsync(request);
        }

        public Task<HttpResponseMessage> ChangePasswordAsync(string token, string current, string next) =>
            SendAsync(HttpMethod.Post, ApiRoutes.ChangePassword, token,
                new ChangePasswordRequest { CurrentPassword = current, NewPassword = next });

        public static async Task AssertErrorAsync(HttpResponseMessage response, HttpStatusCode status, string code)
        {
            Assert.Equal(status, response.StatusCode);
            var error = await response.Content.ReadFromJsonAsync<ApiError>();
            Assert.Equal(code, error!.Code);
            Assert.NotEqual(string.Empty, error.Message);
            Assert.Equal(error.CorrelationId, Assert.Single(response.Headers.GetValues("X-Correlation-ID")));
        }

        public void Dispose()
        {
            Client.Dispose();
            _factory.Dispose();
        }
    }
}
