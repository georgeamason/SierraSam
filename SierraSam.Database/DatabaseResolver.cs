using System.Data;
using SierraSam.Core;
using SierraSam.Database.Databases;

namespace SierraSam.Database;

public static class DatabaseResolver
{
    // TODO: Make use of a proper connection string parser
    public static IDatabase Create(IDbConnection connection, IConfiguration configuration)
    {
        var connectionString = connection.ConnectionString;

        if (connectionString.Contains("postgres", StringComparison.InvariantCultureIgnoreCase))
        {
            return new PostgresDatabase(connection, configuration);
        }

        return new MssqlDatabase(connection, configuration);
    }
}