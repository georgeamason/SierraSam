using System.Data;
using System.Data.Odbc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SierraSam.Core;
using SierraSam.Database.Databases;

namespace SierraSam.Database;

public static class DatabaseResolver
{
    // TODO: Make use of a proper connection string parser
    // TODO: I'm checking explicit ODBC drivers here? Is that a good idea?
    public static IDatabase Create(
        ILoggerFactory loggerFactory,
        IDbConnection connection,
        IDbExecutor executor,
        IConfiguration configuration,
        IMemoryCache cache
    )
    {
        var connectionString = new OdbcConnectionStringBuilder(configuration.Url);

        return connectionString switch
        {
            { Driver: "{MySQL ODBC 8.2 UNICODE Driver}" } =>
                new MysqlDatabase(
                    loggerFactory.CreateLogger<MysqlDatabase>(),
                    connection,
                    executor,
                    configuration,
                    cache
                ),
            { Driver: "{PostgreSQL UNICODE}" } =>
                new PostgresDatabase(
                    loggerFactory.CreateLogger<PostgresDatabase>(),
                    connection,
                    executor,
                    configuration,
                    cache
                ),
            { Driver: "{Oracle 21 ODBC driver}" } =>
                new OracleDatabase(
                    loggerFactory.CreateLogger<OracleDatabase>(),
                    connection,
                    executor,
                    configuration,
                    cache
                ),
            _ => new MssqlDatabase(
                loggerFactory.CreateLogger<MssqlDatabase>(),
                connection,
                executor,
                configuration,
                cache
            )
        };
    }
}