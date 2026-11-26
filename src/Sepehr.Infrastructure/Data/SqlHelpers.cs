using System;
using System.Data;
using Microsoft.Data.SqlClient;

namespace Sepehr.Infrastructure.Data
{
    /// <summary>همه مقادیر فقط به‌صورت پارامتر نوع‌دار فرستاده می‌شوند.</summary>
    internal static class SqlHelpers
    {
        public static SqlCommand Command(SqlConnection connection, string sql, SqlTransaction? transaction = null) =>
            new(sql, connection, transaction);

        public static SqlCommand With(this SqlCommand command, string name, SqlDbType type, object? value, int size = 0)
        {
            var parameter = size == 0
                ? command.Parameters.Add(name, type)
                : command.Parameters.Add(name, type, size);
            if (type == SqlDbType.DateTime2) parameter.Scale = 3;
            parameter.Value = value ?? DBNull.Value;
            return command;
        }

        public static DateTime GetUtc(this SqlDataReader reader, int ordinal) =>
            DateTime.SpecifyKind(reader.GetDateTime(ordinal), DateTimeKind.Utc);
    }
}
