using System.Data.Odbc;
using SierraSam.Core;

namespace SierraSam.Database.Databases;

public class PostgresDatabase : DefaultDatabase
{
    private readonly IConfiguration _configuration;

    private readonly OdbcExecutor _odbcExecutor;

    public PostgresDatabase(OdbcConnection connection, IConfiguration configuration)
        : base(connection, configuration)
    {
        _configuration = configuration
            ?? throw new ArgumentNullException(nameof(configuration));

        _odbcExecutor = new OdbcExecutor(connection);
    }

    public override string Name => "PostgreSQL";

    public override void CreateSchemaHistory(
        string? schema = null,
        string? table = null,
        OdbcTransaction? transaction = null)
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

        _odbcExecutor.ExecuteNonQuery(sql, transaction);
    }

    public override void InsertSchemaHistory(AppliedMigration appliedMigration, OdbcTransaction? transaction = null)
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

        _odbcExecutor.ExecuteNonQuery(sql, transaction);
    }
}