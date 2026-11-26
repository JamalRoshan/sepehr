using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Sepehr.Application.Auth;

namespace Sepehr.Infrastructure.Data
{
    internal sealed class SqlAccountScope : IAccountScope
    {
        private readonly SqlConnection _connection;
        private readonly SqlTransaction _transaction;
        private readonly string _username;

        public SqlAccountScope(
            SqlConnection connection, SqlTransaction transaction, string username, UserAccount? account)
        {
            _connection = connection;
            _transaction = transaction;
            _username = username;
            Account = account;
        }

        public UserAccount? Account { get; }

        private long UserId => Account?.UserId ?? throw new InvalidOperationException("No account in scope.");

        private SqlCommand Command(string sql) => SqlHelpers.Command(_connection, sql, _transaction);

        public async Task<IReadOnlyList<DateTime>> GetRecentFailuresAsync(
            DateTime sinceUtc, CancellationToken cancellationToken)
        {
            var owner = Account != null ? "UserId=@userId" : "UserId IS NULL AND Username=@username";
            await using var command = Command(
                "SELECT AttemptAt FROM dbo.LoginAttempts WHERE " + owner +
                " AND Success=0 AND AttemptAt>=@since AND FailureReasonCode IN (@bad, @badCurrent) " +
                "AND LoginAttemptId > ISNULL((SELECT MAX(LoginAttemptId) FROM dbo.LoginAttempts WHERE " + owner +
                " AND Success=1), 0);")
                .With("@since", SqlDbType.DateTime2, sinceUtc)
                .With("@bad", SqlDbType.VarChar, AttemptReasons.BadCredentials, 40)
                .With("@badCurrent", SqlDbType.VarChar, AttemptReasons.BadCurrentPassword, 40);
            if (Account != null) command.With("@userId", SqlDbType.BigInt, Account.UserId);
            else command.With("@username", SqlDbType.NVarChar, _username, 32);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var failures = new List<DateTime>();
            while (await reader.ReadAsync(cancellationToken)) failures.Add(reader.GetUtc(0));
            return failures;
        }

        public async Task AddAttemptAsync(LoginAttempt attempt, CancellationToken cancellationToken)
        {
            await using var command = Command(
                "INSERT dbo.LoginAttempts(Username, UserId, AttemptAt, IpAddress, Success, FailureReasonCode) " +
                "VALUES (@username, @userId, @at, @ip, @success, @reason);")
                .With("@username", SqlDbType.NVarChar, attempt.Username, 32)
                .With("@userId", SqlDbType.BigInt, attempt.UserId)
                .With("@at", SqlDbType.DateTime2, attempt.AtUtc)
                .With("@ip", SqlDbType.VarChar, attempt.IpAddress, 45)
                .With("@success", SqlDbType.Bit, attempt.Success)
                .With("@reason", SqlDbType.VarChar, attempt.FailureReason, 40);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        public async Task AddSessionAsync(NewSession session, CancellationToken cancellationToken)
        {
            await using var command = Command(
                "INSERT dbo.UserSessions(SessionId, UserId, TokenHash, CreatedAt, ExpiresAt, LastActivityAt, DeviceInfo) " +
                "VALUES (@id, @userId, @hash, @created, @expires, @created, @device);")
                .With("@id", SqlDbType.UniqueIdentifier, session.SessionId)
                .With("@userId", SqlDbType.BigInt, session.UserId)
                .With("@hash", SqlDbType.Binary, session.TokenHash, 32)
                .With("@created", SqlDbType.DateTime2, session.CreatedAtUtc)
                .With("@expires", SqlDbType.DateTime2, session.ExpiresAtUtc)
                .With("@device", SqlDbType.NVarChar, session.DeviceInfo, 256);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        public async Task<bool> IsSessionOpenAsync(Guid sessionId, CancellationToken cancellationToken)
        {
            await using var command = Command(
                "SELECT COUNT(*) FROM dbo.UserSessions " +
                "WHERE SessionId=@id AND UserId=@userId AND RevokedAt IS NULL;")
                .With("@id", SqlDbType.UniqueIdentifier, sessionId)
                .With("@userId", SqlDbType.BigInt, UserId);
            return (int)(await command.ExecuteScalarAsync(cancellationToken))! == 1;
        }

        public async Task SetPasswordAsync(string passwordHash, DateTime nowUtc, CancellationToken cancellationToken)
        {
            await using var command = Command(
                "UPDATE dbo.UserCredentials SET PasswordHash=@hash, PasswordChangedAt=@now WHERE UserId=@userId; " +
                "UPDATE dbo.Users SET MustChangePassword=0, UpdatedAt=@now WHERE UserId=@userId;")
                .With("@hash", SqlDbType.VarChar, passwordHash, 256)
                .With("@now", SqlDbType.DateTime2, nowUtc)
                .With("@userId", SqlDbType.BigInt, UserId);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        public async Task RevokeOtherSessionsAsync(
            Guid keepSessionId, DateTime nowUtc, CancellationToken cancellationToken)
        {
            await using var command = Command(
                "UPDATE dbo.UserSessions SET RevokedAt=@now, SessionStatus=2 " +
                "WHERE UserId=@userId AND SessionId<>@keep AND RevokedAt IS NULL;")
                .With("@now", SqlDbType.DateTime2, nowUtc)
                .With("@userId", SqlDbType.BigInt, UserId)
                .With("@keep", SqlDbType.UniqueIdentifier, keepSessionId);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        public Task AddAuditAsync(AuditEntry entry, CancellationToken cancellationToken) =>
            SqlAuthStore.InsertAuditAsync(_connection, _transaction, entry, cancellationToken);

        public Task CommitAsync(CancellationToken cancellationToken) => _transaction.CommitAsync(cancellationToken);

        public async ValueTask DisposeAsync()
        {
            /* تراکنش Commit‌نشده هنگام Dispose برگردانده می‌شود. */
            await _transaction.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }
}
