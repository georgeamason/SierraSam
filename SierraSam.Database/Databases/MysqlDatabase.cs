using System.Data;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SierraSam.Core;

namespace SierraSam.Database.Databases;

internal sealed class MysqlDatabase : DefaultDatabase
{
    private readonly ILogger<DefaultDatabase> _logger;
    private readonly IDbConnection _connection;
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
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
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

    public override IReadOnlyCollection<AppliedMigration> GetSchemaHistory(
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
                 {(appliedMigration.Version is not null ? $"N'{appliedMigration.Version}'," : "NULL,")}
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

    public override string ServerVersion =>
        _executor.ExecuteScalar<string>("SELECT VERSION()")!;

    public override string DefaultSchema =>
        _executor.ExecuteScalar<string>("SELECT SCHEMA()")!;
}