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
        Connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _dbExecutor = executor ?? throw new ArgumentNullException(nameof(executor));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    public abstract string Provider { get; }

    public abstract string ServerVersion { get; }

    public abstract string DefaultSchema { get; }

    public IDbConnection Connection { get; }

    public virtual bool HasMigrationTable(IDbTransaction? transaction = null) => HasTable(transaction: transaction);

    public virtual bool HasTable(
        string? schema = null,
        string? table = null,
        IDbTransaction? transaction = null
    )
    {
        schema ??= _configuration.DefaultSchema;
        table ??= _configuration.SchemaTable;

        var sql = $"""
                   SELECT "table_name"
                   FROM "information_schema"."tables"
                   WHERE "table_schema" = '{schema}' AND
                         "table_name" = '{table}'
                   """;

        var result = _dbExecutor.ExecuteReader<string>(
            sql,
            reader => reader.GetString(0),
            transaction
        );

        return result.Any();
    }

    public virtual bool HasView(
        string view,
        string? schema = null,
        IDbTransaction? transaction = null
    )
    {
        schema ??= _configuration.DefaultSchema;

        var sql = $"""
                   SELECT "table_name"
                   FROM "information_schema"."views"
                   WHERE "table_schema" = '{schema}' AND
                         "table_name" = '{view}'
                   """;

        var result = _dbExecutor.ExecuteReader<string>(
            sql,
            reader => reader.GetString(0),
            transaction
        );

        return result.Any();
    }

    public virtual bool HasRoutine(
        string routine,
        string? schema = null,
        IDbTransaction? transaction = null
    )
    {
        schema ??= _configuration.DefaultSchema;

        var sql = $"""
                   SELECT "routine_name"
                   FROM "information_schema"."routines"
                   WHERE "routine_schema" = '{schema}' AND
                         "routine_name" = '{routine}'
                   """;

        var result = _dbExecutor.ExecuteReader<string>(
            sql,
            reader => reader.GetString(0),
            transaction
        );

        return result.Any();
    }

    public virtual bool HasDomain(
        string domain,
        string? schema = null,
        IDbTransaction? transaction = null
    )
    {
        schema ??= _configuration.DefaultSchema;

        var sql = $"""
                   SELECT "domain_name"
                   FROM "information_schema"."domains"
                   WHERE "domain_schema" = '{schema}' AND
                         "domain_name" = '{domain}'
                   """;

        var result = _dbExecutor.ExecuteReader<string>(
            sql,
            reader => reader.GetString(0),
            transaction
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

    public virtual IReadOnlyCollection<AppliedMigration> GetAppliedMigrations(
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

        if (HasMigrationTable(transaction) is false)
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
                {(appliedMigration.Version is not null ? $"N'{appliedMigration.Version}'" : "NULL")},
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
        const string cacheKey = "schema_history";

        var sql =
            $"""
             UPDATE "{_configuration.DefaultSchema}"."{_configuration.SchemaTable}"
             SET "version" = {(appliedMigration.Version is not null ? $"N'{appliedMigration.Version}'" : "NULL")},
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

        _cache.Remove(cacheKey);

        return _dbExecutor.ExecuteNonQuery(sql, transaction);
    }

    public virtual TimeSpan ExecuteMigration(string sql, IDbTransaction? transaction = null)
    {
        var stopwatch = Stopwatch.StartNew();
        _dbExecutor.ExecuteNonQuery(sql, transaction);
        stopwatch.Stop();

        return stopwatch.Elapsed;
    }

    public virtual void Clean(
        string? schema = null,
        IDbTransaction? transaction = null)
    {
        schema ??= _configuration.DefaultSchema!;

        // https://learn.microsoft.com/en-us/sql/t-sql/statements/drop-trigger-transact-sql?view=sql-server-ver16#b-dropping-a-ddl-trigger
        // Will not find DDL triggers, they are not schema scoped
        var sql = $"""
                   SELECT "objects"."table_name",
                          "objects"."type",
                          "objects"."parent"
                   FROM (
                       SELECT TABLE_CATALOG,
                              TABLE_SCHEMA,
                              TABLE_NAME,
                              'U'  AS "type",
                              NULL AS PARENT
                       FROM information_schema.tables
                       WHERE TABLE_SCHEMA = '{schema}'
                         AND TABLE_TYPE = 'BASE TABLE'
                       UNION ALL
                       SELECT table_constraints.CONSTRAINT_CATALOG,
                              table_constraints.CONSTRAINT_SCHEMA,
                              table_constraints.CONSTRAINT_NAME,
                              CASE CONSTRAINT_TYPE
                                  WHEN 'CHECK' THEN
                                      'C' -- Auto drop
                                  WHEN 'UNIQUE' THEN
                                      'UQ' -- Auto drop
                                  WHEN 'PRIMARY KEY' THEN
                                      'PK' -- Auto drop
                                  WHEN 'FOREIGN KEY' THEN
                                      'F'
                              END,
                              TABLE_NAME AS PARENT
                       FROM information_schema.table_constraints table_constraints
                       INNER JOIN information_schema.referential_constraints r
                                  ON r.constraint_name = table_constraints.constraint_name
                       WHERE table_constraints.CONSTRAINT_SCHEMA = '{schema}'
                       UNION ALL
                       SELECT TABLE_CATALOG,
                              TABLE_SCHEMA,
                              TABLE_NAME,
                              'V',
                              NULL AS PARENT
                       FROM information_schema.views
                       WHERE TABLE_SCHEMA = '{schema}'
                       UNION ALL
                       SELECT ROUTINE_CATALOG,
                              ROUTINE_SCHEMA,
                              ROUTINE_NAME,
                              CASE ROUTINE_TYPE
                                  WHEN 'PROCEDURE' THEN
                                      'P'
                                  WHEN 'FUNCTION' THEN
                                      'FN'
                              END,
                              NULL AS PARENT
                       FROM information_schema.routines
                       WHERE ROUTINE_SCHEMA = '{schema}'
                       UNION ALL
                       SELECT DOMAIN_CATALOG,
                              DOMAIN_SCHEMA,
                              DOMAIN_NAME,
                              'T',
                              NULL AS PARENT
                        FROM information_schema.domains
                        WHERE DOMAIN_SCHEMA = '{schema}'
                       ) AS "objects"
                   ORDER BY CASE
                        WHEN "objects"."type" IN ('F', 'UQ', 'C') THEN
                            1 -- Constraints
                        WHEN "objects"."type" IN ('PK') THEN
                            2 -- Primary Key
                        WHEN "objects"."type" = 'V' THEN
                            3 -- View
                        WHEN "objects"."type" = 'U' THEN
                            4 -- Table
                        WHEN "objects"."type" = 'P' THEN
                            5 -- Procedure
                        WHEN "objects"."type" = 'FN' THEN
                            6 -- Function
                       WHEN "objects"."type" = 'T' THEN
                            7 -- Domains
                    END;
                   """;

        var objects = _dbExecutor.ExecuteReader<DatabaseObj>(
            sql,
            reader => new DatabaseObj(
                schema,
                reader.GetString(0).Trim(),
                !reader.IsDBNull(1) ? reader.GetString(1).Trim() : null,
                !reader.IsDBNull(2) ? reader.GetString(2).Trim() : null
            ),
            transaction);

        foreach (var obj in objects) DropObject(obj, transaction);
    }

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

    private void DropObject(DatabaseObj obj, IDbTransaction? transaction = null)
    {
        // https://learn.microsoft.com/en-us/sql/relational-databases/system-catalog-views/sys-objects-transact-sql?view=sql-server-ver16
        var sql = obj switch
        {
            { Type: "C" or "F" or "PK" or "UQ" } => $"ALTER TABLE \"{obj.Parent}\" DROP CONSTRAINT \"{obj.Name}\";",
            { Type: "FN" } => $"DROP FUNCTION \"{obj.Name}\";",
            { Type: "P" } => $"DROP PROCEDURE \"{obj.Name}\";",
            { Type: "U" } => $"DROP TABLE \"{obj.Name}\";",
            { Type: "V" } => $"DROP VIEW \"{obj.Name}\";",
            { Type: "T" } => $"DROP TYPE \"{obj.Name}\";",
            _   => throw new ArgumentOutOfRangeException(
                nameof(obj),
                $"Unknown database object type '{obj.Type}' for {obj.Name}"
            )
        };

        _logger.LogInformation("{sql}", sql);

        _dbExecutor.ExecuteNonQuery(sql, transaction);
    }
}