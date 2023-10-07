using System.Data;
using SierraSam.Core;

namespace SierraSam.Database.Databases;

public class PostgresDatabase : DefaultDatabase
{
    private readonly IConfiguration _configuration;
    private readonly IDbExecutor _dbExecutor;

    public PostgresDatabase(
        IDbConnection connection,
        IDbExecutor executor,
        IConfiguration configuration)
        : base(connection, executor, configuration)
    {
        _configuration = configuration
                         ?? throw new ArgumentNullException(nameof(configuration));

        _dbExecutor = executor
            ?? throw new ArgumentNullException(nameof(executor));
    }

    public override string Provider => "PostgreSQL";

    public override void CreateSchemaHistory(
        string? schema = null,
        string? table = null,
        IDbTransaction? transaction = null)
    {
        schema ??= _configuration.DefaultSchema;
        table ??= _configuration.SchemaTable;

        var sql =
            $"CREATE TABLE \"{schema}\".\"{table}\"(" +
            $"\"installed_rank\" INT PRIMARY KEY NOT NULL," +
            $"\"version\" VARCHAR(50) NULL," +
            $"\"description\" VARCHAR(200) NOT NULL," +
            $"\"type\" VARCHAR(20) NOT NULL," +
            $"\"script\" VARCHAR(1000) NOT NULL," +
            $"\"checksum\" VARCHAR(32) NOT NULL," +
            $"\"installed_by\" VARCHAR(100) NOT NULL," +
            $"\"installed_on\" TIMESTAMP NOT NULL DEFAULT (now() at time zone 'utc')," +
            $"\"execution_time\" REAL NOT NULL," +
            $"\"success\" BOOLEAN NOT NULL)";

        _dbExecutor.ExecuteNonQuery(sql, transaction);
    }

    public override void InsertSchemaHistory(AppliedMigration appliedMigration, IDbTransaction? transaction = null)
    {
        var sql =
            $"INSERT INTO \"{_configuration.DefaultSchema}\".\"{_configuration.SchemaTable}\"(" +
                "\"installed_rank\"," +
                "\"version\"," +
                "\"description\"," +
                "\"type\"," +
                "\"script\"," +
                "\"checksum\"," +
                "\"installed_by\"," +
                "\"installed_on\"," +
                "\"execution_time\"," +
                "\"success\")" +
            " VALUES(" +
                $"{appliedMigration.InstalledRank}," +
                (appliedMigration.Version is not null ? $"N'{appliedMigration.Version}'," : $"NULL,") +
                $"N'{appliedMigration.Description}'," +
                $"N'{appliedMigration.Type}'," +
                $"N'{appliedMigration.Script}'," +
                $"N'{appliedMigration.Checksum}'," +
                $"N'{appliedMigration.InstalledBy}'," +
                "DEFAULT," +
                $"{appliedMigration.ExecutionTime}," +
                $"{appliedMigration.Success})";

        _dbExecutor.ExecuteNonQuery(sql, transaction);
    }

    public override string ServerVersion => _dbExecutor.ExecuteScalar<string>("SHOW SERVER_VERSION")!;

    public override string DefaultSchema => _dbExecutor.ExecuteScalar<string>("SELECT CURRENT_SCHEMA()")!;
}