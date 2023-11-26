using System.Data;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SierraSam.Core;

namespace SierraSam.Database.Databases;

public sealed class PostgresDatabase : DefaultDatabase
{
    private readonly ILogger<PostgresDatabase> _logger;
    private readonly IConfiguration _configuration;
    private readonly IMemoryCache _cache;
    private readonly IDbExecutor _dbExecutor;

    public PostgresDatabase(
        ILogger<PostgresDatabase> logger,
        IDbConnection connection,
        IDbExecutor executor,
        IConfiguration configuration,
        IMemoryCache cache)
        : base(logger, connection, executor, configuration, cache)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _dbExecutor = executor ?? throw new ArgumentNullException(nameof(executor));

        _configuration.DefaultSchema ??= this.DefaultSchema;
    }

    public override string Provider => "PostgreSQL";

    public override bool HasTable(
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
                {(appliedMigration.Version is not null ? $"N'{appliedMigration.Version}'" : $"NULL")},
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

    public override int UpdateSchemaHistory(AppliedMigration appliedMigration, IDbTransaction? transaction = null)
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
                 "installed_on"   = N'{appliedMigration.InstalledOn:yyyy-MM-dd HH:mm:ss}'::TIMESTAMP,
                 "execution_time" = N'{appliedMigration.ExecutionTime}'::REAL,
                 "success"        = N'{appliedMigration.Success}'::BOOLEAN
             WHERE "installed_rank" = {appliedMigration.InstalledRank}
             """;

        _cache.Remove(cacheKey);

        return _dbExecutor.ExecuteNonQuery(sql, transaction);
    }

    public override void Clean(string? schema = null, IDbTransaction? transaction = null)
    {
        schema ??= _configuration.DefaultSchema!;

        // ReSharper disable StringLiteralTypo
        var sql = $"""
                    SELECT object_identity AS name,
                           object_type     AS type,
                           _cls.relname    AS parent
                    FROM (
                        WITH RECURSIVE preference AS (
                            SELECT 10         AS max_depth
                                 , '{schema}' AS schema
                            )
                           , dependency_pair      AS (
                            SELECT objid
                                 , ARRAY_AGG(objsubid ORDER BY objsubid)                            AS objsubids
                                 , CASE obj.type
                                       WHEN 'table' THEN 'U'
                                       WHEN 'sequence' THEN 'SO'
                                       WHEN 'view' THEN 'V'
                                       WHEN 'materialized view' THEN 'V'
                                       WHEN 'aggregate' THEN 'AF'
                                       WHEN 'function' THEN 'FN'
                                       WHEN 'procedure' THEN 'P'
                                       WHEN 'type' THEN 'TT'
                                       WHEN 'rule' THEN 'R'
                                       WHEN 'trigger' THEN 'TR'
                                       ELSE obj.type
                                   END                                                              AS object_type
                                 , COALESCE(obj.schema, SUBSTRING(obj.identity, E'(\\w+?)\\.'), '') AS object_schema
                                 , obj.name                                                         AS object_name
                                 , obj.identity                                                     AS object_identity
                                 , refobjid
                                 , ARRAY_AGG(refobjsubid ORDER BY refobjsubid)                      AS refobjsubids
                                 , UPPER(refobj.type)                                               AS refobj_type
                                 , COALESCE(CASE
                                                WHEN refobj.type = 'schema' THEN refobj.identity
                                                ELSE refobj.schema
                                            END
                                , SUBSTRING(refobj.identity, E'(\\w+?)\\.'), '')                    AS refobj_schema
                                 , refobj.name                                                      AS refobj_name
                                 , refobj.identity                                                  AS refobj_identity
                                 , CASE deptype
                                       WHEN 'n' THEN 'normal'
                                       WHEN 'a' THEN 'automatic'
                                       WHEN 'i' THEN 'internal'
                                       WHEN 'e' THEN 'extension'
                                       WHEN 'p' THEN 'pinned'
                                   END                                                              AS dependency_type
                            FROM pg_depend dep
                               , LATERAL PG_IDENTIFY_OBJECT(classid, objid, 0) AS obj
                               , LATERAL PG_IDENTIFY_OBJECT(refclassid, refobjid, 0) AS refobj
                               , preference
                            WHERE deptype IN ('n','a')
                              AND COALESCE(obj.schema, SUBSTRING(obj.identity, E'(\\w+?)\\.'), '') = preference.schema
                            GROUP BY objid, obj.type, obj.schema, obj.name, obj.identity
                                   , refobjid, refobj.type, refobj.schema, refobj.name, refobj.identity, deptype
                            )
                           , dependency_hierarchy AS (
                            SELECT DISTINCT 0                AS level,
                                            refobjid         AS objid,
                                            refobj_type      AS object_type,
                                            refobj_identity  AS object_identity,
                                            NULL::text       AS dependency_type,
                                            ARRAY [refobjid] AS dependency_chain
                            FROM dependency_pair root
                               , preference
                            WHERE NOT EXISTS
                                (
                                    SELECT 'x' FROM dependency_pair branch WHERE branch.objid = root.refobjid
                                    )
                              AND refobj_schema = preference.schema
                            UNION ALL
                            SELECT level + 1 AS level,
                                   child.objid,
                                   child.object_type,
                                   child.object_identity,
                                   child.dependency_type,
                                   parent.dependency_chain || child.objid
                            FROM dependency_pair child
                                 JOIN dependency_hierarchy parent ON (parent.objid = child.refobjid)
                               , preference
                            WHERE level < preference.max_depth
                              AND child.object_schema = preference.schema
                              AND NOT (child.objid = ANY (parent.dependency_chain)) -- prevent circular referencing
                            )
                        SELECT ROW_NUMBER() OVER (PARTITION BY objid ORDER BY level DESC, dependency_chain DESC) AS drop_order,
                               *
                        FROM dependency_hierarchy
                        ) AS dep
                    LEFT JOIN pg_class _cls ON _cls.oid = dep.dependency_chain[ARRAY_LENGTH(dep.dependency_chain, 1) - 1]
                    WHERE drop_order = 1
                      AND dep.dependency_type = 'normal'
                      AND NOT (dep.object_type = 'R' AND dep.object_identity ~ E'^"_RETURN"') -- ignore view default rule
                    ORDER BY level DESC, dependency_chain DESC;
                    """;
        // ReSharper restore StringLiteralTypo

        var objects = _dbExecutor.ExecuteReader<DatabaseObject>(
            sql,
            reader => new DatabaseObject(
                schema,
                reader.GetString(0).Trim(),
                !reader.IsDBNull(1) ? reader.GetString(1).Trim() : null,
                !reader.IsDBNull(2) ? reader.GetString(2).Trim() : null),
            transaction);

        foreach (var obj in objects) DropObject(obj, transaction);
    }

    public override string ServerVersion =>
        _dbExecutor.ExecuteScalar<string>("SHOW SERVER_VERSION")!;

    public override string DefaultSchema =>
        _dbExecutor.ExecuteScalar<string>("SELECT CURRENT_SCHEMA()")!;
}