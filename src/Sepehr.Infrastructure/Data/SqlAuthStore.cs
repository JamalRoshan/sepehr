using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Sepehr.Application.Auth;
using Sepehr.Domain;

namespace Sepehr.Infrastructure.Data
{
    public sealed class SqlAuthStore : IAuthStore
    {
        private readonly string _connectionString;

        public SqlAuthStore(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("Connection string is required.", nameof(connectionString));
            _connectionString = connectionString;
        }

        private async Task<SqlConnection> OpenAsync(CancellationToken cancellationToken)
        {
            var connection = new SqlConnection(_connectionString);
            try
            {
                await connection.OpenAsync(cancellationToken);
                return connection;
            }
            catch
            {
                await connection.DisposeAsync();
                throw;
            }
        }

        public async Task<SecurityOptions> GetSecurityOptionsAsync(CancellationToken cancellationToken)
        {
            var values = new Dictionary<string, string>(StringComparer.Ordinal);
            await using var connection = await OpenAsync(cancellationToken);
            await using var command = SqlHelpers.Command(connection,
                "SELECT SettingKey, SettingValue FROM dbo.SystemSettings " +
                "WHERE SettingKey LIKE N'Session.%' OR SettingKey LIKE N'Login.%';");
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
                values[reader.GetString(0)] = reader.GetString(1);

            /* مقدار نامعتبر یا خارج از بازه به پیش‌فرض امن برمی‌گردد. */
            int Read(string key, int fallback, int minimum, int maximum) =>
                values.TryGetValue(key, out var text)
                && int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var number)
                && number >= minimum && number <= maximum
                    ? number
                    : fallback;

            return new SecurityOptions(
                new SessionOptions(
                    TimeSpan.FromMinutes(Read("Session.IdleMinutes", 30, 1, 1440)),
                    TimeSpan.FromHours(Read("Session.AbsoluteHours", 12, 1, 168))),
                new LockoutOptions(
                    Read("Login.MaxFailures", 5, 1, 20),
                    TimeSpan.FromMinutes(Read("Login.WindowMinutes", 15, 1, 1440)),
                    TimeSpan.FromMinutes(Read("Login.LockMinutes", 15, 1, 1440))));
        }

        public async Task<IAccountScope?> LockAccountAsync(string username, CancellationToken cancellationToken)
        {
            var connection = await OpenAsync(cancellationToken);
            try
            {
                var transaction = (SqlTransaction)await connection.BeginTransactionAsync(
                    IsolationLevel.ReadCommitted, cancellationToken);

                /* کلید قفل از شناسه کاربر ساخته می‌شود، چون Collation چند نوشتار نام را یکی می‌داند. */
                var userId = await FindUserIdAsync(connection, transaction, username, cancellationToken);
                var key = userId != null
                    ? "id:" + userId.Value.ToString(CultureInfo.InvariantCulture)
                    : "name:" + username.TrimEnd().ToUpperInvariant();
                var resource = "Sepehr.Account." + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(key)));

                await using (var lockCommand = SqlHelpers.Command(connection,
                    "DECLARE @result int; " +
                    "EXEC @result = sys.sp_getapplock @Resource=@resource, @LockMode='Exclusive', " +
                    "@LockOwner='Transaction', @LockTimeout=10000; SELECT @result;", transaction)
                    .With("@resource", SqlDbType.NVarChar, resource, 255))
                {
                    var result = (int)(await lockCommand.ExecuteScalarAsync(cancellationToken))!;
                    if (result < 0)
                    {
                        await connection.DisposeAsync();
                        return null;
                    }
                }

                /* حساب پس از گرفتن قفل خوانده می‌شود تا رمز و وضعیت تازه باشند. */
                var account = userId == null
                    ? null
                    : await ReadAccountAsync(connection, transaction, userId.Value, cancellationToken);

                return new SqlAccountScope(connection, transaction, username, account);
            }
            catch
            {
                await connection.DisposeAsync();
                throw;
            }
        }

        private static async Task<long?> FindUserIdAsync(
            SqlConnection connection, SqlTransaction transaction, string username, CancellationToken cancellationToken)
        {
            await using var command = SqlHelpers.Command(connection,
                "SELECT UserId FROM dbo.Users WHERE Username=@username;", transaction)
                .With("@username", SqlDbType.NVarChar, username, 32);
            return (long?)await command.ExecuteScalarAsync(cancellationToken);
        }

        private static async Task<UserAccount?> ReadAccountAsync(
            SqlConnection connection, SqlTransaction transaction, long userId, CancellationToken cancellationToken)
        {
            await using var command = SqlHelpers.Command(connection,
                "SELECT u.UserId, u.Username, u.PersonnelCode, c.PasswordHash, u.IsActive, u.MustChangePassword " +
                "FROM dbo.Users u JOIN dbo.UserCredentials c ON c.UserId=u.UserId WHERE u.UserId=@userId;",
                transaction)
                .With("@userId", SqlDbType.BigInt, userId);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken)) return null;

            return new UserAccount(reader.GetInt64(0), reader.GetString(1), reader.GetString(2),
                reader.GetString(3), reader.GetBoolean(4), reader.GetBoolean(5));
        }

        public async Task<SessionRecord?> FindSessionAsync(byte[] tokenHash, CancellationToken cancellationToken)
        {
            await using var connection = await OpenAsync(cancellationToken);
            await using var command = SqlHelpers.Command(connection,
                "SELECT s.SessionId, s.UserId, u.Username, u.DisplayName, u.IsActive, u.MustChangePassword, " +
                "s.CreatedAt, s.ExpiresAt, s.LastActivityAt, s.RevokedAt " +
                "FROM dbo.UserSessions s JOIN dbo.Users u ON u.UserId=s.UserId WHERE s.TokenHash=@hash;")
                .With("@hash", SqlDbType.Binary, tokenHash, 32);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken)) return null;

            return new SessionRecord(reader.GetGuid(0), reader.GetInt64(1), reader.GetString(2),
                reader.GetString(3), reader.GetBoolean(4), reader.GetBoolean(5),
                reader.GetUtc(6), reader.GetUtc(7), reader.GetUtc(8),
                reader.IsDBNull(9) ? null : reader.GetUtc(9));
        }

        public async Task TouchSessionAsync(Guid sessionId, DateTime nowUtc, CancellationToken cancellationToken)
        {
            await using var connection = await OpenAsync(cancellationToken);
            await using var command = SqlHelpers.Command(connection,
                "UPDATE dbo.UserSessions SET LastActivityAt=@now " +
                "WHERE SessionId=@id AND RevokedAt IS NULL AND LastActivityAt<@now;")
                .With("@now", SqlDbType.DateTime2, nowUtc)
                .With("@id", SqlDbType.UniqueIdentifier, sessionId);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<string>> GetGlobalRolesAsync(long userId, CancellationToken cancellationToken)
        {
            await using var connection = await OpenAsync(cancellationToken);
            await using var command = SqlHelpers.Command(connection,
                "SELECT r.RoleCode FROM dbo.UserRoles ur JOIN dbo.Roles r ON r.RoleId=ur.RoleId " +
                "WHERE ur.UserId=@userId ORDER BY r.RoleCode;")
                .With("@userId", SqlDbType.BigInt, userId);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var roles = new List<string>();
            while (await reader.ReadAsync(cancellationToken)) roles.Add(reader.GetString(0));
            return roles;
        }

        public async Task<IReadOnlyList<SessionSummary>> GetOpenSessionsAsync(
            long userId, DateTime nowUtc, TimeSpan idleTimeout, CancellationToken cancellationToken)
        {
            await using var connection = await OpenAsync(cancellationToken);
            await using var command = SqlHelpers.Command(connection,
                "SELECT SessionId, CreatedAt, LastActivityAt, ExpiresAt, DeviceInfo FROM dbo.UserSessions " +
                "WHERE UserId=@userId AND RevokedAt IS NULL AND ExpiresAt>@now AND LastActivityAt>@idleLimit " +
                "ORDER BY CreatedAt DESC;")
                .With("@userId", SqlDbType.BigInt, userId)
                .With("@now", SqlDbType.DateTime2, nowUtc)
                .With("@idleLimit", SqlDbType.DateTime2, nowUtc - idleTimeout);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var sessions = new List<SessionSummary>();
            while (await reader.ReadAsync(cancellationToken))
                sessions.Add(new SessionSummary(reader.GetGuid(0), reader.GetUtc(1), reader.GetUtc(2),
                    reader.GetUtc(3), reader.IsDBNull(4) ? null : reader.GetString(4)));
            return sessions;
        }

        public async Task RevokeSessionAsync(Guid sessionId, AuditEntry audit, CancellationToken cancellationToken)
        {
            await using var connection = await OpenAsync(cancellationToken);
            await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);
            await using (var command = SqlHelpers.Command(connection,
                "UPDATE dbo.UserSessions SET RevokedAt=@now, SessionStatus=2 " +
                "WHERE SessionId=@id AND RevokedAt IS NULL;", transaction)
                .With("@now", SqlDbType.DateTime2, audit.AtUtc)
                .With("@id", SqlDbType.UniqueIdentifier, sessionId))
            {
                await command.ExecuteNonQueryAsync(cancellationToken);
            }

            await InsertAuditAsync(connection, transaction, audit, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }

        internal static async Task InsertAuditAsync(
            SqlConnection connection, SqlTransaction transaction, AuditEntry entry,
            CancellationToken cancellationToken)
        {
            await using var command = SqlHelpers.Command(connection,
                "INSERT dbo.AuditLogs(ActorUserId, Action, EntityType, EntityId, [Timestamp], CorrelationId, Result) " +
                "VALUES (@actor, @action, 'User', @entity, @at, @correlation, @result);", transaction)
                .With("@actor", SqlDbType.BigInt, entry.ActorUserId)
                .With("@action", SqlDbType.VarChar, entry.Action, 80)
                .With("@entity", SqlDbType.NVarChar,
                    entry.TargetUserId?.ToString(CultureInfo.InvariantCulture), 64)
                .With("@at", SqlDbType.DateTime2, entry.AtUtc)
                .With("@correlation", SqlDbType.UniqueIdentifier, entry.CorrelationId)
                .With("@result", SqlDbType.TinyInt, (byte)(entry.Success ? 1 : 2));
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }
}
