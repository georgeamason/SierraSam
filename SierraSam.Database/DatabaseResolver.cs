using System.Data;
using SierraSam.Core;
using SierraSam.Database.Databases;

namespace SierraSam.Database;

public static class DatabaseResolver
{
    // TODO: Make use of a proper connection string parser
    public static IDatabase Create(
        IDbConnection connection,
        IDbExecutor executor,
        IConfiguration configuration)
    {
        var connectionString = configuration.Url;

        if (connectionString.Contains("postgres", StringComparison.InvariantCultureIgnoreCase))
        {
            return new PostgresDatabase(connection, executor, configuration);
        }

        return new MssqlDatabase(connection, executor, configuration);
    }
}