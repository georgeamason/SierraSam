using System.Data;
using System.Diagnostics;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SierraSam.Core;

namespace SierraSam.Database;

public abstract class DefaultDatabase : IDatabase
{
    private readonly ILogger<DefaultDatabase> _logger;
    private readonly IConfiguration _configuration;
    private readonly IMemoryCache _cache;
    private readonly IDbExecutor _dbExecutor;

    protected DefaultDatabase(
        ILogger<DefaultDatabase> logger,
        IDbConnection connection,
        IDbExecutor executor,
        IConfiguration configuration,
        IMemoryCache cache)
    {
        Connection = connection
            ?? throw new ArgumentNullException(nameof(connection));

        _logger = logger
            ?? throw new ArgumentNullException(nameof(logger));

        _dbExecutor = executor
            ?? throw new ArgumentNullException(nameof(executor));

        _configuration = configuration
            ?? throw new ArgumentNullException(nameof(configuration));

        _cache = cache
            ?? throw new ArgumentNullException(nameof(cache));
    }

    public abstract string Provider { get; }

    public abstract string ServerVersion { get; }

    public abstract string DefaultSchema { get; }

    public IDbConnection Connection { get; }

    public virtual bool HasMigrationTable => HasTable(_configuration.SchemaTable);

    public virtual bool HasTable(string tableName)
    {
        var sql = $"""
                   SELECT "TABLE_NAME"
                   FROM "INFORMATION_SCHEMA"."TABLES"
                   WHERE "TABLE_NAME" = '{tableName}'
                   """;

        var result = _dbExecutor.ExecuteReader<string>(
            sql,
            reader => reader.GetString(0)
        );

        return result.Any();
    }

    public virtual void CreateSchemaHistory(
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
                "version" NVARCHAR(50) NULL,
                "description" NVARCHAR(200) NOT NULL,
                "type" NVARCHAR(20) NOT NULL,
                "script" NVARCHAR(1000) NOT NULL,
                "checksum" NVARCHAR(32) NOT NULL,
                "installed_by" NVARCHAR(100) NOT NULL,
                "installed_on" DATETIME NOT NULL DEFAULT (GETUTCDATE()),
                "execution_time" FLOAT NOT NULL,
                "success" BIT NOT NULL
             )
             """;

        _dbExecutor.ExecuteNonQuery(sql, transaction);
    }

    public virtual IReadOnlyCollection<AppliedMigration> GetSchemaHistory(
        string? schema = null,
        string? table = null,
        IDbTransaction? transaction = null)
    {
        const string cacheKey = "schema_history";

        if (_cache.TryGetValue(cacheKey, out IReadOnlyCollection<AppliedMigration>? appliedMigrations))
        {
            _logger.LogDebug("Using cached schema history");
            return appliedMigrations ?? Array.Empty<AppliedMigration>();
        }

        schema ??= _configuration.DefaultSchema;
        table ??= _configuration.SchemaTable;

        var sql =
            $"""
             SELECT 
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
              FROM "{schema}"."{table}"
              ORDER BY "installed_rank"
             """;

        if (HasMigrationTable is false)
        {
            throw new InvalidOperationException($"Schema history table " +
                                                $"\"{_configuration.DefaultSchema}\".\"{_configuration.SchemaTable}\" " +
                                                $"does not exist");
        }

        _logger.LogDebug("Fetching schema history from database");

        return _cache.Set(
            cacheKey,
            _dbExecutor.ExecuteReader<AppliedMigration>(
                sql,
                reader => new AppliedMigration(
                    reader.GetInt32(0),
                    !reader.IsDBNull(1) ? reader.GetString(1) : null,
                    reader.GetString(2),
                    reader.GetString(3),
                    reader.GetString(4),
                    reader.GetString(5),
                    reader.GetString(6),
                    new DateTime(
                        reader.GetDateTime(7).Ticks,
                        DateTimeKind.Utc
                    ),
                    reader.GetDouble(8),
                    reader.GetBoolean(9)
                ),
                transaction
            )
        );
    }

    public virtual int InsertSchemaHistory(AppliedMigration appliedMigration, IDbTransaction? transaction = null)
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
                {(appliedMigration.Version is not null ? $"N'{appliedMigration.Version}'," : "NULL,")}
                N'{appliedMigration.Description}',
                N'{appliedMigration.Type}',
                N'{appliedMigration.Script}',
                N'{appliedMigration.Checksum}',
                N'{appliedMigration.InstalledBy}',
                DEFAULT,
                {appliedMigration.ExecutionTime},
                {(appliedMigration.Success ? 1 : 0)}
             )
             """;

        _cache.Remove(cacheKey);

        return _dbExecutor.ExecuteNonQuery(sql, transaction);
    }

    public virtual int UpdateSchemaHistory(AppliedMigration appliedMigration, IDbTransaction? transaction = null)
    {
        var sql =
            $"""
             UPDATE "{_configuration.DefaultSchema}"."{_configuration.SchemaTable}"
             SET "version" = {(appliedMigration.Version is not null ? $"N'{appliedMigration.Version}'," : "NULL,")}
                 "description" = N'{appliedMigration.Description}',
                 "type" = N'{appliedMigration.Type}',
                 "script" = N'{appliedMigration.Script}',
                 "checksum" = N'{appliedMigration.Checksum}',
                 "installed_by"   = N'{appliedMigration.InstalledBy}',
                 "installed_on"   = N'{appliedMigration.InstalledOn:yyyy-MM-dd HH:mm:ss}',
                 "execution_time" = N'{appliedMigration.ExecutionTime}',
                 "success"        = N'{appliedMigration.Success}'
             WHERE "installed_rank" = {appliedMigration.InstalledRank}
             """;

        return _dbExecutor.ExecuteNonQuery(sql, transaction);
    }

    public virtual TimeSpan ExecuteMigration(string sql, IDbTransaction? transaction = null)
    {
        var stopwatch = Stopwatch.StartNew();
        _dbExecutor.ExecuteNonQuery(sql, transaction);
        stopwatch.Stop();

        return stopwatch.Elapsed;
    }

    public abstract IEnumerable<string> Clean(IDbTransaction? transaction = null);

    public virtual int GetInstalledRank(
        string? schema = null,
        string? table = null,
        IDbTransaction? transaction = null
    )
    {
        schema ??= _configuration.DefaultSchema;
        table ??= _configuration.SchemaTable;

        var sql = $"SELECT MAX(\"installed_rank\") FROM \"{schema}\".\"{table}\"";

        return _dbExecutor.ExecuteScalar<int>(sql, transaction);
    }
}