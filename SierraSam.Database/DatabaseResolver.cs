using System.Data;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SierraSam.Core;
using SierraSam.Database.Databases;

namespace SierraSam.Database;

public static class DatabaseResolver
{
    // TODO: Make use of a proper connection string parser
    public static IDatabase Create(
        ILoggerFactory loggerFactory,
        IDbConnection connection,
        IDbExecutor executor,
        IConfiguration configuration,
        IMemoryCache cache)
    {
        var connectionString = configuration.Url;

        if (connectionString.Contains("mysql", StringComparison.InvariantCultureIgnoreCase))
        {
            return new MysqlDatabase(
                loggerFactory.CreateLogger<MysqlDatabase>(),
                connection,
                executor,
                configuration,
                cache
            );
        }

        if (connectionString.Contains("postgres", StringComparison.InvariantCultureIgnoreCase))
        {
            return new PostgresDatabase(
                loggerFactory.CreateLogger<PostgresDatabase>(),
                connection,
                executor,
                configuration,
                cache
            );
        }

        return new MssqlDatabase(
            loggerFactory.CreateLogger<MssqlDatabase>(),
            connection,
            executor,
            configuration,
            cache
        );
    }
}