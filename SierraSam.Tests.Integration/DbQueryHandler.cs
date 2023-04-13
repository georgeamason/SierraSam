using System.Data;
using System.Data.Odbc;
using SierraSam.Core.Extensions;

namespace SierraSam.Tests.Integration;

internal static class DbQueryHandler
{
    public static DataTable? ExecuteSql
        (string connectionString, string sql)
    {
        using var odbcConnection = new OdbcConnection(connectionString);
        using var command = new OdbcCommand(sql, odbcConnection);

        odbcConnection.Open();

        using var dataReader = command.ExecuteReader();

        return dataReader.GetData();
    }

    public static bool HasRow
        (string connectionString,
         string table)
    {
        using var odbcConnection = new OdbcConnection(connectionString);
        using var command = new OdbcCommand();

        command.Connection = odbcConnection;
        command.CommandText = $"SELECT COUNT(*) FROM {table}";
        command.CommandType = CommandType.Text;

        odbcConnection.Open();
        var rows = command.ExecuteScalar();

        return rows is not DBNull;
    }
}