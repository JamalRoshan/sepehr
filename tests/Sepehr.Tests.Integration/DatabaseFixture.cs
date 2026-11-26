using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Sepehr.Infrastructure.Security;
using Xunit;

namespace Sepehr.Tests.Integration
{
    /// <summary>
    /// دیتابیس موقت روی SQL Server واقعی. متغیر SEPEHR_TEST_SQL باید تنظیم شده باشد؛
    /// نبود آن خطاست و آزمون‌ها بی‌صدا رد نمی‌شوند.
    /// </summary>
    public sealed class DatabaseFixture : IAsyncLifetime
    {
        private readonly string _databaseName = "SepehrTest_" + Guid.NewGuid().ToString("N");
        private string _serverConnectionString = string.Empty;

        public string ConnectionString { get; private set; } = string.Empty;

        public async Task InitializeAsync()
        {
            _serverConnectionString = Environment.GetEnvironmentVariable("SEPEHR_TEST_SQL")
                ?? throw new InvalidOperationException("SEPEHR_TEST_SQL is not set.");

            await ExecuteAsync(_serverConnectionString,
                $"CREATE DATABASE [{_databaseName}] COLLATE Persian_100_CI_AS_SC;");

            ConnectionString = new SqlConnectionStringBuilder(_serverConnectionString)
            {
                InitialCatalog = _databaseName
            }.ConnectionString;

            var script = await File.ReadAllTextAsync(Path.Combine(
                RepositoryRoot(), "database", "migrations", "0001_initial.sql"));
            await using var connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync();
            foreach (var batch in Regex.Split(script, @"^\s*GO\s*$", RegexOptions.Multiline))
            {
                if (string.IsNullOrWhiteSpace(batch)) continue;
                await using var command = new SqlCommand(batch, connection);
                await command.ExecuteNonQueryAsync();
            }
        }

        public async Task DisposeAsync()
        {
            SqlConnection.ClearAllPools();
            await ExecuteAsync(_serverConnectionString,
                $"ALTER DATABASE [{_databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; " +
                $"DROP DATABASE [{_databaseName}];");
        }

        /// <summary>کاربر آزمایشی با رمز اولیه برابر کد پرسنلی.</summary>
        public async Task<TestUser> CreateUserAsync()
        {
            var code = "P" + Guid.NewGuid().ToString("N").Substring(0, 5);
            var userId = (long)(await ScalarAsync(
                "INSERT dbo.Users(PersonnelCode, Username, DisplayName, SearchText) " +
                "VALUES (@p0, @p0, N'کاربر آزمایشی', N'کاربر آزمایشی'); SELECT CONVERT(bigint, SCOPE_IDENTITY());",
                code))!;
            await ScalarAsync("INSERT dbo.UserCredentials(UserId, PasswordHash) VALUES (@p0, @p1);",
                userId, PasswordHasher.Hash(code));
            return new TestUser(userId, code);
        }

        /// <summary>پارامترها با نام @p0، @p1 و ... در SQL می‌آیند.</summary>
        public async Task<object?> ScalarAsync(string sql, params object[] parameters)
        {
            await using var connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync();
            await using var command = new SqlCommand(sql, connection);
            for (var i = 0; i < parameters.Length; i++)
                command.Parameters.AddWithValue("@p" + i, parameters[i]);
            return await command.ExecuteScalarAsync();
        }

        public async Task<List<string>> RowsAsTextAsync(string sql)
        {
            await using var connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync();
            await using var command = new SqlCommand(sql, connection);
            await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleResult);
            var rows = new List<string>();
            while (await reader.ReadAsync())
            {
                var values = new object[reader.FieldCount];
                reader.GetValues(values);
                rows.Add(string.Join("|", values));
            }

            return rows;
        }

        private static async Task ExecuteAsync(string connectionString, string sql)
        {
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            await using var command = new SqlCommand(sql, connection);
            await command.ExecuteNonQueryAsync();
        }

        private static string RepositoryRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "Sepehr.sln")))
                directory = directory.Parent;
            return directory?.FullName ?? throw new InvalidOperationException("Sepehr.sln not found.");
        }
    }

    public sealed record TestUser(long UserId, string Code);

    [CollectionDefinition(Name)]
    public sealed class DatabaseCollection : ICollectionFixture<DatabaseFixture>
    {
        public const string Name = "database";
    }
}
