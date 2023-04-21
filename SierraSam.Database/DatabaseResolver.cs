using System.Data.Odbc;
using SierraSam.Core;
using SierraSam.Database.Databases;

namespace SierraSam.Database;

public static class DatabaseFactory
{
    public static IDatabase Create(OdbcConnection odbcConnection, Configuration configuration)
    {
        var connectionString = odbcConnection.ConnectionString;

        if (connectionString.Contains("postgres", StringComparison.InvariantCultureIgnoreCase))
        {
            return new PostgresDatabase(odbcConnection, configuration);
        }

        return new MssqlDatabase(odbcConnection, configuration);
    }
}