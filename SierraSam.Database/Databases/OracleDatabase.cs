using System.Data;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SierraSam.Core;

namespace SierraSam.Database.Databases;

internal sealed class OracleDatabase : DefaultDatabase
{
    private readonly ILogger<DefaultDatabase> _logger;
    private readonly IDbExecutor _executor;
    private readonly IConfiguration _configuration;
    private readonly IMemoryCache _cache;

    public OracleDatabase(
        ILogger<DefaultDatabase> logger,
        IDbConnection connection,
        IDbExecutor executor,
        IConfiguration configuration,
        IMemoryCache cache)
        : base(logger, connection, executor, configuration, cache)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _executor = executor ?? throw new ArgumentNullException(nameof(executor));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));

        _configuration.DefaultSchema ??= this.DefaultSchema;
    }

    public override string Provider => "Oracle";

    public override string ServerVersion =>
        _executor.ExecuteScalar<string>("SELECT \"VERSION_FULL\" FROM \"PUBLIC\".\"V$INSTANCE\"")!;

    public override string DefaultSchema =>
        _executor.ExecuteScalar<string>("SELECT SYS_CONTEXT('USERENV', 'CURRENT_SCHEMA') FROM \"PUBLIC\".\"DUAL\"")!;

    public override bool HasTable(
        string? schema = null,
        string? table = null,
        IDbTransaction? transaction = null
    )
    {
        schema ??= _configuration.DefaultSchema;
        table ??= _configuration.SchemaTable;

        var sql = $"""
                   SELECT "TABLE_NAME"
                   FROM "PUBLIC"."ALL_TABLES"
                   WHERE "OWNER"='{schema}' AND
                         "TABLE_NAME"='{table}'
                   """;

        var result = _executor.ExecuteReader<string>(
            sql,
            reader => reader.GetString(0),
            transaction
        );

        return result.Any();
    }

    public override void CreateSchemaHistory(
        string? schema = null,
        string? table = null,
        IDbTransaction? transaction = null
    )
    {
        schema ??= _configuration.DefaultSchema;
        table ??= _configuration.SchemaTable;

        // Not possible to specify an INSTALLED_ON default
        // https://docs.oracle.com/en/database/oracle/oracle-database/23/adfns/odbc-driver.html#GUID-3FE69BEF-F8D2-4152-9B1A-877186C47028
        var sql = $"""
                   CREATE TABLE "{schema}"."{table}" (
                       "INSTALLED_RANK" NUMBER(9) NOT NULL,
                       "VERSION" NVARCHAR2(50) NULL,
                       "DESCRIPTION" NVARCHAR2(200) NOT NULL,
                       "TYPE" NVARCHAR2(20) NOT NULL,
                       "SCRIPT" NVARCHAR2(1000) NOT NULL,
                       "CHECKSUM" NVARCHAR2(32) NOT NULL,
                       "INSTALLED_BY" NVARCHAR2(100) NOT NULL,
                       "INSTALLED_ON" TIMESTAMP(3) WITH TIME ZONE NOT NULL,
                       "EXECUTION_TIME" NUMBER(8, 4) NOT NULL,
                       "SUCCESS" NUMBER(1,0) NOT NULL,
                       CONSTRAINT "PK_{table}" PRIMARY KEY ("INSTALLED_RANK")
                   )
                   """;

        _executor.ExecuteNonQuery(sql, transaction);
    }

    public override IReadOnlyCollection<AppliedMigration> GetAppliedMigrations(
        string? schema = null,
        string? table = null,
        IDbTransaction? transaction = null
    )
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
             "INSTALLED_RANK",
             "VERSION",
             "DESCRIPTION",
             "TYPE",
             "SCRIPT",
             "CHECKSUM",
             "INSTALLED_BY",
             "INSTALLED_ON",
             "EXECUTION_TIME",
             "SUCCESS"
             FROM "{schema}"."{table}"
             ORDER BY "INSTALLED_RANK"
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
            _executor.ExecuteReader<AppliedMigration>(
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

    public override int GetInstalledRank(
        string? schema = null,
        string? table = null,
        IDbTransaction? transaction = null
    )
    {
        schema ??= _configuration.DefaultSchema;
        table ??= _configuration.SchemaTable;

        // Oracle NUMBER maps to dotnet double
        var installedRank = _executor.ExecuteScalar<double>(
            $"SELECT MAX(\"INSTALLED_RANK\") FROM \"{schema}\".\"{table}\"",
            transaction
        );

        // Shouldn't ever overflow as int.MaxValue is 2,147,483,647
        // and NUMBER(9) is 999,999,999
        return Convert.ToInt32(installedRank);
    }

    public override int InsertSchemaHistory(AppliedMigration appliedMigration, IDbTransaction? transaction = null)
    {
        const string cacheKey = "schema_history";

        var sql =
            $"""
             INSERT INTO "{_configuration.DefaultSchema}"."{_configuration.SchemaTable}"
             (
                "INSTALLED_RANK",
                "VERSION",
                "DESCRIPTION",
                "TYPE",
                "SCRIPT",
                "CHECKSUM",
                "INSTALLED_BY",
                "INSTALLED_ON",
                "EXECUTION_TIME",
                "SUCCESS"
             ) VALUES (
                {appliedMigration.InstalledRank},
                {(appliedMigration.Version is not null ? $"UNISTR('{appliedMigration.Version}')" : "NULL")},
                UNISTR('{appliedMigration.Description}'),
                UNISTR('{appliedMigration.Type}'),
                UNISTR('{appliedMigration.Script}'),
                UNISTR('{appliedMigration.Checksum}'),
                UNISTR('{appliedMigration.InstalledBy}'),
                TIMESTAMP '{appliedMigration.InstalledOn:yyyy-MM-dd HH:mm:ss.fff zzz}',
                {appliedMigration.ExecutionTime},
                {(appliedMigration.Success ? 1 : 0)}
             )
             """;

        _cache.Remove(cacheKey);

        return _executor.ExecuteNonQuery(sql, transaction);
    }

    public override int UpdateSchemaHistory(AppliedMigration appliedMigration, IDbTransaction? transaction = null)
    {
        const string cacheKey = "schema_history";

        var sql =
            $"""
             UPDATE "{_configuration.DefaultSchema}"."{_configuration.SchemaTable}"
             SET "VERSION" = {(appliedMigration.Version is not null ? $"UNISTR('{appliedMigration.Version}')" : "NULL")},
                 "DESCRIPTION" = UNISTR('{appliedMigration.Description}'),
                 "TYPE" = UNISTR('{appliedMigration.Type}'),
                 "SCRIPT" = UNISTR('{appliedMigration.Script}'),
                 "CHECKSUM" = UNISTR('{appliedMigration.Checksum}'),
                 "INSTALLED_BY" = UNISTR('{appliedMigration.InstalledBy}'),
                 "INSTALLED_ON" = TIMESTAMP '{appliedMigration.InstalledOn:yyyy-MM-dd HH:mm:ss.fff zzz}',
                 "EXECUTION_TIME" = {appliedMigration.ExecutionTime},
                 "SUCCESS" = {(appliedMigration.Success ? 1 : 0)}
             WHERE "INSTALLED_RANK" = {appliedMigration.InstalledRank}
             """;

        _cache.Remove(cacheKey);

        return _executor.ExecuteNonQuery(sql, transaction);
    }

    public override void Clean(string? schema = null, IDbTransaction? transaction = null)
    {
        schema ??= _configuration.DefaultSchema!;

        if (schema == "SYSTEM") throw new InvalidOperationException("Cannot clean SYSTEM schema");

        var sql = $"""
                   SELECT "TABLE_NAME",
                          "type",
                          "PARENT"
                   FROM (
                       SELECT OWNER,
                              TABLE_NAME,
                              'U'  AS "type",
                              NULL AS PARENT
                       FROM "PUBLIC".ALL_TABLES
                       WHERE OWNER = '{schema}'
                       UNION ALL
                       SELECT OWNER,
                              CONSTRAINT_NAME,
                              CASE CONSTRAINT_TYPE
                                  WHEN 'C' THEN
                                      'C' -- Auto drop
                                  WHEN 'P' THEN
                                      'PK' -- Auto drop
                                  WHEN 'R' THEN
                                      'F'
                              END,
                              TABLE_NAME AS PARENT
                       FROM "PUBLIC".ALL_CONSTRAINTS
                       WHERE OWNER = '{schema}'
                         AND CONSTRAINT_TYPE = 'R'
                       UNION ALL
                       SELECT OWNER,
                              VIEW_NAME,
                              'V',
                              NULL AS PARENT
                       FROM "PUBLIC".ALL_VIEWS
                       WHERE OWNER = '{schema}'
                       
                       UNION ALL
                       SELECT OWNER,
                              OBJECT_NAME,
                              CASE OBJECT_TYPE
                                  WHEN 'PROCEDURE' THEN
                                      'P'
                                  WHEN 'FUNCTION' THEN
                                      'FN'
                              END,
                              NULL AS PARENT
                       FROM "PUBLIC".ALL_PROCEDURES
                       WHERE OWNER = '{schema}'
                       UNION ALL
                      SELECT OWNER,
                             TYPE_NAME,
                             'T',
                             NULL AS PARENT
                        FROM "PUBLIC".ALL_TYPES
                        WHERE OWNER = '{schema}'
                       )
                   ORDER BY CASE
                       WHEN "type" IN ('F', 'UQ', 'C') THEN
                           1 -- Constraints
                       WHEN "type" IN ('PK') THEN
                           2 -- Primary Key
                       WHEN "type" = 'V' THEN
                           3 -- View
                       WHEN "type" = 'U' THEN
                           4 -- Table
                       WHEN "type" = 'P' THEN
                           5 -- Procedure
                       WHEN "type" = 'FN' THEN
                           6 -- Function
                       WHEN "type" = 'T' THEN
                           7 -- Types
                   END;
                   """;

        var objects = _executor.ExecuteReader<DatabaseObj>(
            sql,
            reader => new DatabaseObj(
                schema,
                reader.GetString(0).Trim(),
                !reader.IsDBNull(1) ? reader.GetString(1).Trim() : null,
                !reader.IsDBNull(2) ? reader.GetString(2).Trim() : null
            ),
            transaction);

        // TODO: Transactions ?
        foreach (var obj in objects) DropObject(obj, transaction);
    }

    private void DropObject(DatabaseObj obj, IDbTransaction? transaction = null)
    {
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

        _executor.ExecuteNonQuery(sql, transaction);
    }
}