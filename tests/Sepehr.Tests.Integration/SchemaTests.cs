using System;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Xunit;

namespace Sepehr.Tests.Integration
{
    [Collection(DatabaseCollection.Name)]
    [Trait("Category", "Integration")]
    public sealed class SchemaTests
    {
        private readonly DatabaseFixture _database;

        public SchemaTests(DatabaseFixture database) => _database = database;

        private async Task<long> AddGroupAsync(long creator, long? parent) =>
            (long)(await _database.ScalarAsync(
                "INSERT dbo.Groups(ParentGroupId, Name, SearchText, CreatedBy) VALUES (@p0, N'g', N'g', @p1); " +
                "SELECT CONVERT(bigint, SCOPE_IDENTITY());",
                (object?)parent ?? DBNull.Value, creator))!;

        [Fact]
        public async Task Group_tree_is_limited_to_three_levels()
        {
            var user = await _database.CreateUserAsync();
            var root = await AddGroupAsync(user.UserId, null);
            var child = await AddGroupAsync(user.UserId, root);
            var grandchild = await AddGroupAsync(user.UserId, child);

            var tooDeep = await Assert.ThrowsAsync<SqlException>(() => AddGroupAsync(user.UserId, grandchild));
            Assert.Equal(51002, tooDeep.Number);

            /* جابه‌جایی زیردرختی که عمق نوادگانش را از حد بگذراند هم رد می‌شود. */
            var otherRoot = await AddGroupAsync(user.UserId, null);
            var otherChild = await AddGroupAsync(user.UserId, otherRoot);
            var moved = await Assert.ThrowsAsync<SqlException>(() => _database.ScalarAsync(
                "UPDATE dbo.Groups SET ParentGroupId=@p0 WHERE GroupId=@p1;", otherChild, child));
            Assert.Equal(51002, moved.Number);
        }

        [Fact]
        public async Task Sync_sequence_follows_commit_order()
        {
            var user = await _database.CreateUserAsync();

            await using var first = new SqlConnection(_database.ConnectionString);
            await using var second = new SqlConnection(_database.ConnectionString);
            await first.OpenAsync();
            await second.OpenAsync();
            await using var firstTransaction = (SqlTransaction)await first.BeginTransactionAsync();
            await using var secondTransaction = (SqlTransaction)await second.BeginTransactionAsync();

            await AppendAsync(first, firstTransaction, "first", user.UserId);
            var blocked = AppendAsync(second, secondTransaction, "second", user.UserId);

            Assert.NotSame(blocked, await Task.WhenAny(blocked, Task.Delay(TimeSpan.FromSeconds(2))));

            await firstTransaction.CommitAsync();
            await blocked;
            await secondTransaction.CommitAsync();

            var order = await _database.RowsAsTextAsync(
                "SELECT EntityId FROM dbo.ChangeLog WHERE EntityId IN (N'first', N'second') ORDER BY Seq;");
            Assert.Equal(new[] { "first", "second" }, order);
        }

        private static async Task AppendAsync(
            SqlConnection connection, SqlTransaction transaction, string entity, long userId)
        {
            await using var command = new SqlCommand("dbo.usp_AppendChange", connection, transaction)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@ChangeType", (byte)1);
            command.Parameters.AddWithValue("@EntityId", entity);
            command.Parameters.AddWithValue("@TargetUserId", userId);
            await command.ExecuteNonQueryAsync();
        }
    }
}
