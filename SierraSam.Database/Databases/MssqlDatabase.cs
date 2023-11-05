using System.Data;
using System.Data.Common;
using System.Data.Odbc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Respawn;
using SierraSam.Core;

namespace SierraSam.Database.Databases;

public sealed class MssqlDatabase : DefaultDatabase
{
    private readonly ILogger<MssqlDatabase> _logger;
    private readonly IConfiguration _configuration;
    private readonly IDbExecutor _dbExecutor;
    private readonly Respawner _respawner;

    public MssqlDatabase(
        ILogger<MssqlDatabase> logger,
        IDbConnection connection,
        IDbExecutor executor,
        IConfiguration configuration,
        IMemoryCache cache)
        : base(logger, connection, executor, configuration, cache)
    {
        _logger = logger
            ?? throw new ArgumentNullException(nameof(logger));

        _configuration = configuration
            ?? throw new ArgumentNullException(nameof(configuration));

        _dbExecutor = executor
            ?? throw new ArgumentNullException(nameof(executor));

        _configuration.DefaultSchema ??= this.DefaultSchema;

        _respawner = Respawner.CreateAsync(
            (OdbcConnection)connection,
            new RespawnerOptions
            {
                DbAdapter = DbAdapter.SqlServer,
                SchemasToInclude = _configuration.Schemas.Any()
                    ? _configuration.Schemas.ToArray()
                    : new[] { _configuration.DefaultSchema }
            }
        ).Result;
    }

    public override string Provider => "MSSQL";

    public override string ServerVersion =>
        _dbExecutor.ExecuteScalar<string>("SELECT SERVERPROPERTY('productversion')")!;

    public override string DefaultSchema =>
        _dbExecutor.ExecuteScalar<string>("SELECT SCHEMA_NAME()")!;

    public override IEnumerable<string> Clean(IDbTransaction? transaction = null)
    {
        if (_respawner.DeleteSql is null) return Array.Empty<string>();

        _logger.LogInformation(_respawner.DeleteSql);

        var sql = _respawner.DeleteSql.Replace("DELETE", "DROP TABLE");
        _dbExecutor.ExecuteNonQuery(sql, transaction);

        return _respawner.Options.SchemasToInclude;
    }
}