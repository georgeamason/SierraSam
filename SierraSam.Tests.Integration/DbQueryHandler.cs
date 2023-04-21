using System.Data;
using System.Data.Common;
using System.Data.Odbc;
using SierraSam.Core.Extensions;

namespace SierraSam.Tests.Integration;

internal static class DbQueryHandler
{
    public static DataTable ExecuteSql(string connectionString, string sql)
    {
        using var odbcConnection = new OdbcConnection(connectionString);
        using var odbcCommand = new OdbcCommand(sql, odbcConnection);
        using var dataAdapter = new OdbcDataAdapter(odbcCommand);

        var dataSet = new DataSet();
        dataAdapter.Fill(dataSet);

        return dataSet.Tables[0];
    }
}