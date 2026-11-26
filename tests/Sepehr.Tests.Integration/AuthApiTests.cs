using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using Sepehr.Contracts;
using Xunit;

namespace Sepehr.Tests.Integration
{
    [Collection(DatabaseCollection.Name)]
    [Trait("Category", "Integration")]
    public sealed class AuthApiTests
    {
        private const string NewPassword = "Strong-pass-1";
        private readonly DatabaseFixture _database;

        public AuthApiTests(DatabaseFixture database) => _database = database;

        private static async Task AssertStatusAsync(
            TestApi api, string path, string token, HttpStatusCode expected) =>
            Assert.Equal(expected, (await api.SendAsync(HttpMethod.Get, path, token)).StatusCode);

        [Fact]
        public async Task First_login_requires_password_change_before_other_access()
        {
            using var api = new TestApi(_database);
            var user = await _database.CreateUserAsync();

            var login = await api.LoginOkAsync(user.Code, user.Code);
            Assert.True(login.MustChangePassword);
            await AssertStatusAsync(api, ApiRoutes.Me, login.Token, HttpStatusCode.OK);
            await TestApi.AssertErrorAsync(
                await api.SendAsync(HttpMethod.Get, ApiRoutes.Sessions, login.Token),
                HttpStatusCode.Forbidden, ErrorCodes.PasswordChangeRequired);

            await TestApi.AssertErrorAsync(await api.ChangePasswordAsync(login.Token, user.Code, "1234567"),
                HttpStatusCode.BadRequest, ErrorCodes.PasswordTooShort);
            await TestApi.AssertErrorAsync(await api.ChangePasswordAsync(login.Token, "wrong-one", NewPassword),
                HttpStatusCode.BadRequest, ErrorCodes.CurrentPasswordIncorrect);
            Assert.Equal(HttpStatusCode.NoContent,
                (await api.ChangePasswordAsync(login.Token, user.Code, NewPassword)).StatusCode);

            await AssertStatusAsync(api, ApiRoutes.Sessions, login.Token, HttpStatusCode.OK);
            await TestApi.AssertErrorAsync(await api.LoginAsync(user.Code, user.Code),
                HttpStatusCode.Unauthorized, ErrorCodes.InvalidCredentials);
            Assert.False((await api.LoginOkAsync(user.Code, NewPassword)).MustChangePassword);
        }

        [Fact]
        public async Task Wrong_password_and_unknown_user_give_the_same_answer()
        {
            using var api = new TestApi(_database);
            var user = await _database.CreateUserAsync();

            var wrong = await api.LoginAsync(user.Code, "not-the-password");
            var unknown = await api.LoginAsync("nobody-" + user.Code, "not-the-password");

            var first = await wrong.Content.ReadFromJsonAsync<ApiError>();
            var second = await unknown.Content.ReadFromJsonAsync<ApiError>();
            Assert.Equal(wrong.StatusCode, unknown.StatusCode);
            Assert.Equal(first!.Code, second!.Code);
            Assert.Equal(first.Message, second.Message);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Five_failures_lock_for_fifteen_minutes(bool existingUser)
        {
            using var api = new TestApi(_database);
            var user = await _database.CreateUserAsync();
            var name = existingUser ? user.Code : "ghost-" + user.Code;

            for (var i = 0; i < 5; i++)
                await TestApi.AssertErrorAsync(await api.LoginAsync(name, "bad-password"),
                    HttpStatusCode.Unauthorized, ErrorCodes.InvalidCredentials);

            var locked = await api.LoginAsync(name, user.Code);
            await TestApi.AssertErrorAsync(locked, HttpStatusCode.TooManyRequests, ErrorCodes.AccountLocked);
            Assert.NotNull(locked.Headers.RetryAfter);

            /* تلاش هنگام قفل، قفل را تمدید نمی‌کند. */
            api.Clock.Advance(TimeSpan.FromMinutes(10));
            await api.LoginAsync(name, "bad-password");
            api.Clock.Advance(TimeSpan.FromMinutes(5.1));

            var after = await api.LoginAsync(name, user.Code);
            Assert.Equal(existingUser ? HttpStatusCode.OK : HttpStatusCode.Unauthorized, after.StatusCode);
        }

        [Fact]
        public async Task Parallel_guesses_cannot_pass_the_failure_limit()
        {
            using var api = new TestApi(_database);
            var user = await _database.CreateUserAsync();

            var responses = await Task.WhenAll(Enumerable.Range(0, 15)
                .Select(_ => api.LoginAsync(user.Code, "bad-password")));

            Assert.Equal(5, responses.Count(r => r.StatusCode == HttpStatusCode.Unauthorized));
            Assert.Equal(10, responses.Count(r => r.StatusCode == HttpStatusCode.TooManyRequests));
            Assert.Equal(5, (int)(await _database.ScalarAsync(
                "SELECT COUNT(*) FROM dbo.LoginAttempts WHERE UserId=@p0 AND FailureReasonCode='BadCredentials';",
                user.UserId))!);
        }

        [Fact]
        public async Task Wrong_current_password_counts_towards_lockout()
        {
            using var api = new TestApi(_database);
            var user = await _database.CreateUserAsync();
            var login = await api.LoginOkAsync(user.Code, user.Code);

            for (var i = 0; i < 5; i++)
                await api.ChangePasswordAsync(login.Token, "bad-password", NewPassword);

            await TestApi.AssertErrorAsync(await api.ChangePasswordAsync(login.Token, user.Code, NewPassword),
                HttpStatusCode.TooManyRequests, ErrorCodes.AccountLocked);
        }

        [Fact]
        public async Task Session_expires_when_idle_but_stays_while_active()
        {
            using var api = new TestApi(_database);
            var user = await _database.CreateUserAsync();
            var login = await api.LoginOkAsync(user.Code, user.Code);

            for (var i = 0; i < 3; i++)
            {
                api.Clock.Advance(TimeSpan.FromMinutes(20));
                await AssertStatusAsync(api, ApiRoutes.Me, login.Token, HttpStatusCode.OK);
            }

            api.Clock.Advance(TimeSpan.FromMinutes(31));
            await AssertStatusAsync(api, ApiRoutes.Me, login.Token, HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Session_ends_after_twelve_hours_even_when_active()
        {
            using var api = new TestApi(_database);
            var user = await _database.CreateUserAsync();
            var login = await api.LoginOkAsync(user.Code, user.Code);

            for (var minutes = 20; minutes < 12 * 60; minutes += 20)
            {
                api.Clock.Advance(TimeSpan.FromMinutes(20));
                await AssertStatusAsync(api, ApiRoutes.Me, login.Token, HttpStatusCode.OK);
            }

            api.Clock.Advance(TimeSpan.FromMinutes(21));
            await AssertStatusAsync(api, ApiRoutes.Me, login.Token, HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Logout_and_password_change_revoke_the_right_sessions()
        {
            using var api = new TestApi(_database);
            var user = await _database.CreateUserAsync();
            var first = await api.LoginOkAsync(user.Code, user.Code);
            var second = await api.LoginOkAsync(user.Code, user.Code);
            var third = await api.LoginOkAsync(user.Code, user.Code);

            Assert.Equal(HttpStatusCode.NoContent,
                (await api.SendAsync(HttpMethod.Post, ApiRoutes.Logout, third.Token)).StatusCode);
            await AssertStatusAsync(api, ApiRoutes.Me, third.Token, HttpStatusCode.Unauthorized);
            await AssertStatusAsync(api, ApiRoutes.Me, second.Token, HttpStatusCode.OK);

            await api.ChangePasswordAsync(first.Token, user.Code, NewPassword);
            await AssertStatusAsync(api, ApiRoutes.Me, second.Token, HttpStatusCode.Unauthorized);

            var response = await api.SendAsync(HttpMethod.Get, ApiRoutes.Sessions, first.Token);
            var sessions = await response.Content.ReadFromJsonAsync<SessionInfo[]>();
            Assert.True(Assert.Single(sessions!).IsCurrent);
        }

        [Fact]
        public async Task Disabled_user_loses_access()
        {
            using var api = new TestApi(_database);
            var user = await _database.CreateUserAsync();
            var login = await api.LoginOkAsync(user.Code, user.Code);

            await _database.ScalarAsync("UPDATE dbo.Users SET IsActive=0 WHERE UserId=@p0;", user.UserId);

            await AssertStatusAsync(api, ApiRoutes.Me, login.Token, HttpStatusCode.Unauthorized);
            await TestApi.AssertErrorAsync(await api.LoginAsync(user.Code, user.Code),
                HttpStatusCode.Unauthorized, ErrorCodes.InvalidCredentials);
        }

        [Fact]
        public async Task Password_is_not_normalized()
        {
            using var api = new TestApi(_database);
            var user = await _database.CreateUserAsync();
            var login = await api.LoginOkAsync(user.Code, user.Code);
            const string arabicYeh = "رمز-عربي-123";
            await api.ChangePasswordAsync(login.Token, user.Code, arabicYeh);

            Assert.Equal(HttpStatusCode.OK, (await api.LoginAsync(user.Code, arabicYeh)).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized,
                (await api.LoginAsync(user.Code, arabicYeh.Replace('ي', 'ی'))).StatusCode);
        }

        [Fact]
        public async Task Token_and_password_are_not_stored_in_clear()
        {
            using var api = new TestApi(_database);
            var user = await _database.CreateUserAsync();
            var login = await api.LoginOkAsync(user.Code, user.Code);
            await api.ChangePasswordAsync(login.Token, user.Code, NewPassword);

            var audit = await _database.RowsAsTextAsync(
                "SELECT Action, EntityId, Metadata, Result FROM dbo.AuditLogs;");
            Assert.Contains(audit, row => row.StartsWith("Auth.LoginSucceeded|" + user.UserId + "|"));
            Assert.Contains(audit, row => row.StartsWith("Auth.PasswordChanged|" + user.UserId + "|"));
            Assert.All(audit, row =>
            {
                Assert.DoesNotContain(login.Token, row);
                Assert.DoesNotContain(NewPassword, row);
            });

            var hash = (string)(await _database.ScalarAsync(
                "SELECT PasswordHash FROM dbo.UserCredentials WHERE UserId=@p0;", user.UserId))!;
            Assert.StartsWith("pbkdf2-sha256$600000$", hash);
            var stored = (byte[])(await _database.ScalarAsync(
                "SELECT TOP (1) TokenHash FROM dbo.UserSessions WHERE UserId=@p0;", user.UserId))!;
            Assert.Equal(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(login.Token)), stored);
        }

        [Fact]
        public async Task Login_rate_limit_is_enforced()
        {
            using var api = new TestApi(_database, loginPerMinute: 3);
            for (var i = 0; i < 3; i++)
                Assert.Equal(HttpStatusCode.Unauthorized, (await api.LoginAsync("rate-" + i, "x")).StatusCode);

            await TestApi.AssertErrorAsync(await api.LoginAsync("rate-4", "x"),
                HttpStatusCode.TooManyRequests, ErrorCodes.TooManyRequests);
        }

        [Fact]
        public async Task Plain_http_is_rejected()
        {
            using var api = new TestApi(_database);
            using var client = api.CreateClient("http://localhost");

            var response = await client.PostAsJsonAsync(ApiRoutes.Login,
                new LoginRequest { Username = "a", Password = "b" });

            await TestApi.AssertErrorAsync(response, HttpStatusCode.BadRequest, ErrorCodes.HttpsRequired);
        }

        [Theory]
        [InlineData("{not json")]
        [InlineData("{\"username\":null,\"password\":null}")]
        [InlineData("{\"username\":\"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa\",\"password\":\"x\"}")]
        public async Task Invalid_input_gets_a_safe_error(string body)
        {
            using var api = new TestApi(_database);

            var response = await api.Client.PostAsync(ApiRoutes.Login,
                new StringContent(body, Encoding.UTF8, "application/json"));

            await TestApi.AssertErrorAsync(response, HttpStatusCode.BadRequest, ErrorCodes.InvalidRequest);
        }

        [Fact]
        public async Task Missing_or_forged_token_is_unauthorized()
        {
            using var api = new TestApi(_database);

            await TestApi.AssertErrorAsync(await api.Client.GetAsync(ApiRoutes.Me),
                HttpStatusCode.Unauthorized, ErrorCodes.Unauthorized);
            await TestApi.AssertErrorAsync(await api.SendAsync(HttpMethod.Get, ApiRoutes.Me, "forged-token"),
                HttpStatusCode.Unauthorized, ErrorCodes.Unauthorized);
        }
    }
}
