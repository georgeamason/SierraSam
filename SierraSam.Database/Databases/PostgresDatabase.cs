using System.Data;
using System.Data.Odbc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Respawn;
using SierraSam.Core;

namespace SierraSam.Database.Databases;

public sealed class PostgresDatabase : DefaultDatabase
{
    private readonly ILogger<PostgresDatabase> _logger;
    private readonly IConfiguration _configuration;
    private readonly IMemoryCache _cache;
    private readonly IDbExecutor _dbExecutor;
    private readonly Respawner _respawner;

    public PostgresDatabase(
        ILogger<PostgresDatabase> logger,
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

        _cache = cache
            ?? throw new ArgumentNullException(nameof(cache));

        _dbExecutor = executor
            ?? throw new ArgumentNullException(nameof(executor));

        _configuration.DefaultSchema ??= this.DefaultSchema;

        _respawner = Respawner.CreateAsync(
            (OdbcConnection) connection,
            new RespawnerOptions
            {
                DbAdapter = DbAdapter.Postgres,
                SchemasToInclude = _configuration.Schemas.Any()
                    ? _configuration.Schemas.ToArray()
                    : new[] { _configuration.DefaultSchema }
            }
        ).Result;
    }

    public override string Provider => "PostgreSQL";

    public override bool HasTable(string tableName)
    {
        var sql = $"""
                   SELECT "table_name"
                   FROM "information_schema"."tables"
                   WHERE "table_name" = '{tableName}'
                   """;

        var result = _dbExecutor.ExecuteReader<string>(
            sql,
            reader => reader.GetString(0)
        );

        return result.Any();
    }

    public override void CreateSchemaHistory(
        string? schema = null,
        string? table = null,
        IDbTransaction? transaction = null)
    {
        schema ??= _configuration.DefaultSchema;
        table ??= _configuration.SchemaTable;

        var sql =
            $"""
             CREATE TABLE "{schema}"."{table}" (
                "installed_rank" INT PRIMARY KEY NOT NULL,
                "version" VARCHAR(50) NULL,
                "description" VARCHAR(200) NOT NULL,
                "type" VARCHAR(20) NOT NULL,
                "script" VARCHAR(1000) NOT NULL,
                "checksum" VARCHAR(32) NOT NULL,
                "installed_by" VARCHAR(100) NOT NULL,
                "installed_on" TIMESTAMP NOT NULL DEFAULT (now() at time zone 'utc'),
                "execution_time" REAL NOT NULL,
                "success" BOOLEAN NOT NULL
             )
             """;

        _dbExecutor.ExecuteNonQuery(sql, transaction);
    }

    public override int InsertSchemaHistory(AppliedMigration appliedMigration, IDbTransaction? transaction = null)
    {
        const string cacheKey = "schema_history";

        var sql =
            $"""
             INSERT INTO "{_configuration.DefaultSchema}"."{_configuration.SchemaTable}"
             (
                "installed_rank",
                "version",
                "description",
                "type",
                "script",
                "checksum",
                "installed_by",
                "installed_on",
                "execution_time",
                "success"
             ) VALUES (
                {appliedMigration.InstalledRank},
                {(appliedMigration.Version is not null ? $"N'{appliedMigration.Version}'," : $"NULL,")}
                N'{appliedMigration.Description}',
                N'{appliedMigration.Type}',
                N'{appliedMigration.Script}',
                N'{appliedMigration.Checksum}',
                N'{appliedMigration.InstalledBy}',
                DEFAULT,
                {appliedMigration.ExecutionTime},
                {appliedMigration.Success}
             )
             """;

        _cache.Remove(cacheKey);

        return _dbExecutor.ExecuteNonQuery(sql, transaction);
    }

    public override IEnumerable<string> Clean(IDbTransaction? transaction = null)
    {
        if (string.IsNullOrEmpty(_respawner.DeleteSql)) return Array.Empty<string>();

        _dbExecutor.ExecuteNonQuery(_respawner.DeleteSql, transaction);

        return _respawner.Options.SchemasToInclude;
    }

    public override string ServerVersion =>
        _dbExecutor.ExecuteScalar<string>("SHOW SERVER_VERSION")!;

    public override string DefaultSchema =>
        _dbExecutor.ExecuteScalar<string>("SELECT CURRENT_SCHEMA()")!;
}