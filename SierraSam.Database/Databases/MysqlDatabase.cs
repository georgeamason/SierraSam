﻿using System.Data;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SierraSam.Core;

namespace SierraSam.Database.Databases;

internal sealed class MysqlDatabase : DefaultDatabase
{
    private readonly ILogger<DefaultDatabase> _logger;
    private readonly IDbExecutor _executor;
    private readonly IConfiguration _configuration;
    private readonly IMemoryCache _cache;

    public MysqlDatabase(
        ILogger<DefaultDatabase> logger,
        IDbConnection connection,
        IDbExecutor executor,
        IConfiguration configuration,
        IMemoryCache cache
    ) : base(logger, connection, executor, configuration, cache)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _executor = executor ?? throw new ArgumentNullException(nameof(executor));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));

        _configuration.DefaultSchema ??= this.DefaultSchema;
    }

    public override string Provider => "MySQL";

    public override bool HasTable(
        string? schema = null,
        string? table = null,
        IDbTransaction? transaction = null
    )
    {
        schema ??= _configuration.DefaultSchema;
        table ??= _configuration.SchemaTable;

        var sql = $"""
                   SELECT `table_name`
                   FROM `information_schema`.`tables`
                   WHERE `table_schema` = '{schema}' AND
                         `table_name` = '{table}'
                   """;

        var result = _executor.ExecuteReader<string>(
            sql,
            reader => reader.GetString(0),
            transaction
        );

        return result.Any();
    }

    public override bool HasView(
        string view,
        string? schema = null,
        IDbTransaction? transaction = null
    )
    {
        schema ??= _configuration.DefaultSchema;

        var sql = $"""
                   SELECT `table_name`
                   FROM `information_schema`.`views`
                   WHERE `table_schema` = '{schema}' AND
                         `table_name` = '{view}'
                   """;

        var result = _executor.ExecuteReader<string>(
            sql,
            reader => reader.GetString(0),
            transaction
        );

        return result.Any();
    }
    
    public override bool HasRoutine(
        string routine,
        string? schema = null,
        IDbTransaction? transaction = null
    )
    {
        schema ??= _configuration.DefaultSchema;

        var sql = $"""
                   SELECT `routine_name`
                   FROM `information_schema`.`routines`
                   WHERE `routine_schema` = '{schema}' AND
                         `routine_name` = '{routine}'
                   """;

        var result = _executor.ExecuteReader<string>(
            sql,
            reader => reader.GetString(0),
            transaction
        );

        return result.Any();
    }

    public override bool HasDomain(string domain, string? schema = null, IDbTransaction? transaction = null)
        => throw new NotSupportedException("MySQL does not support domains");

    public override void CreateSchemaHistory(
        string? schema = null,
        string? table = null,
        IDbTransaction? transaction = null)
    {
        schema ??= _configuration.DefaultSchema;
        table ??= _configuration.SchemaTable;

        var sql =
            $"""
             CREATE TABLE `{schema}`.`{table}` (
                `installed_rank` INT PRIMARY KEY NOT NULL,
                `version` VARCHAR(50) NULL,
                `description` VARCHAR(200) NOT NULL,
                `type` VARCHAR(20) NOT NULL,
                `script` VARCHAR(1000) NOT NULL,
                `checksum` VARCHAR(32) NOT NULL,
                `installed_by` VARCHAR(100) NOT NULL,
                `installed_on` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                `execution_time` REAL NOT NULL,
                `success` BOOLEAN NOT NULL
             );
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
                `installed_rank`,
                `version`,
                `description`,
                `type`,
                `script`,
                `checksum`,
                `installed_by`,
                `installed_on`,
                `execution_time`,
                `success`
              FROM `{schema}`.`{table}`
              ORDER BY `installed_rank`
              """;

        if (HasMigrationTable(transaction) is false) return Array.Empty<AppliedMigration>();

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
            ));
    }

    public override int InsertSchemaHistory(AppliedMigration appliedMigration, IDbTransaction? transaction = null)
    {
        const string cacheKey = "schema_history";

        var sql =
            $"""
             INSERT INTO `{_configuration.DefaultSchema}`.`{_configuration.SchemaTable}`
             (
                 `installed_rank`,
                 `version`,
                 `description`,
                 `type`,
                 `script`,
                 `checksum`,
                 `installed_by`,
                 `installed_on`,
                 `execution_time`,
                 `success`
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
                 {appliedMigration.Success}
             )
             """;

        _cache.Remove(cacheKey);

        return _executor.ExecuteNonQuery(sql, transaction);
    }

    public override int GetInstalledRank(
        string? schema = null,
        string? table = null,
        IDbTransaction? transaction = null
    )
    {
        schema ??= _configuration.DefaultSchema;
        table ??= _configuration.SchemaTable;

        var sql = $"SELECT MAX(`installed_rank`) FROM `{schema}`.`{table}`";

        return _executor.ExecuteScalar<int>(sql, transaction);
    }

    public override int UpdateSchemaHistory(AppliedMigration appliedMigration, IDbTransaction? transaction = null)
    {
        const string cacheKey = "schema_history";

        var sql =
            $"""
             UPDATE `{_configuration.DefaultSchema}`.`{_configuration.SchemaTable}`
             SET `version` = {(appliedMigration.Version is not null ? $"N'{appliedMigration.Version}'" : "NULL")},
                 `description` = N'{appliedMigration.Description}',
                 `type` = N'{appliedMigration.Type}',
                 `script` = N'{appliedMigration.Script}',
                 `checksum` = N'{appliedMigration.Checksum}',
                 `installed_by` = N'{appliedMigration.InstalledBy}',
                 `installed_on` = N'{appliedMigration.InstalledOn:yyyy-MM-dd HH:mm:ss}',
                 `execution_time` = {appliedMigration.ExecutionTime},
                 `success` = {appliedMigration.Success}
             WHERE `installed_rank` = {appliedMigration.InstalledRank}
             """;

        _cache.Remove(cacheKey);

        return _executor.ExecuteNonQuery(sql, transaction);
    }

    public override void Clean(string? schema = null, IDbTransaction? transaction = null)
    {
        schema ??= _configuration.DefaultSchema!;

        // https://learn.microsoft.com/en-us/sql/t-sql/statements/drop-trigger-transact-sql?view=sql-server-ver16#b-dropping-a-ddl-trigger
        // Will not find DDL triggers, they are not schema scoped
        var sql = $"""
                   SELECT `objects`.`table_name`,
                          `objects`.`type`,
                          `objects`.`parent`
                   FROM (
                       SELECT TABLE_CATALOG,
                              TABLE_SCHEMA,
                              TABLE_NAME,
                              'U'  AS `type`,
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
                              table_constraints.TABLE_NAME AS PARENT
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
                       ) AS `objects`
                   ORDER BY CASE
                       WHEN `objects`.`type` IN ('F', 'UQ', 'C') THEN
                           1 -- Constraints
                       WHEN `objects`.`type` IN ('PK') THEN
                           2 -- Primary Key
                       WHEN `objects`.`type` = 'V' THEN
                           3 -- View
                       WHEN `objects`.`type` = 'U' THEN
                           4 -- Table
                       WHEN `objects`.`type` = 'P' THEN
                           5 -- Procedure
                       WHEN `objects`.`type` = 'FN' THEN
                           6 -- Function
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

        foreach (var obj in objects) DropObject(obj, transaction);
    }

    public override string ServerVersion =>
        _executor.ExecuteScalar<string>("SELECT VERSION()")!;

    public override string DefaultSchema =>
        _executor.ExecuteScalar<string>("SELECT SCHEMA()")!;
    
    private void DropObject(DatabaseObj obj, IDbTransaction? transaction = null)
    {
        var sql = obj switch
        {
            { Type: "C" or "D" or "PK" or "UQ" } => $"ALTER TABLE `{obj.Schema}`.`{obj.Parent}` DROP CONSTRAINT `{obj.Name}`;",
            { Type: "F" } => $"ALTER TABLE `{obj.Schema}`.`{obj.Parent}` DROP FOREIGN KEY `{obj.Name}`;",
            { Type: "FN" or "IF" or "TF" } => $"DROP FUNCTION `{obj.Schema}`.`{obj.Name}`;",
            { Type: "P" } => $"DROP PROCEDURE `{obj.Schema}`.`{obj.Name}`;",
            { Type: "U" } => $"DROP TABLE `{obj.Schema}`.`{obj.Name}`;",
            { Type: "V" } => $"DROP VIEW `{obj.Schema}`.`{obj.Name}`;",
            _   => throw new ArgumentOutOfRangeException(
                nameof(obj),
                $"Unknown database object type '{obj.Type}' for {obj.Name}"
            )
        };

        _logger.LogInformation("{sql}", sql);

        _executor.ExecuteNonQuery(sql, transaction);
    }
}