using System.Data;
using System.Data.Odbc;
using SierraSam.Core;

namespace SierraSam.Database.Databases;

public class PostgresDatabase : DefaultDatabase
{
    private readonly Configuration _configuration;

    private readonly OdbcExecutor _odbcExecutor;

    public PostgresDatabase(OdbcConnection connection, Configuration configuration)
        : base(connection, configuration)
    {
        _configuration = configuration
            ?? throw new ArgumentNullException(nameof(configuration));

        _odbcExecutor = new OdbcExecutor(connection);
    }

    public override string Name => "PostgreSQL";

    public override void CreateSchemaHistory(string schema, string table)
    {
        var sql =
            $"CREATE TABLE {schema}.{table}(" +
            $"\"installed_rank\" INT PRIMARY KEY NOT NULL," +
            $"\"version\" VARCHAR(50) NULL," +
            $"\"description\" VARCHAR(200) NULL," +
            $"\"type\" VARCHAR(20) NOT NULL," +
            $"\"script\" VARCHAR(1000) NOT NULL," +
            $"\"checksum\" VARCHAR(32) NULL," +
            $"\"installed_by\" VARCHAR(100) NOT NULL," +
            $"\"installed_on\" TIMESTAMP NOT NULL DEFAULT now()," +
            $"\"execution_time\" REAL NOT NULL," +
            $"\"success\" BOOLEAN NOT NULL)";

        _odbcExecutor.ExecuteNonQuery(sql);
    }

    public override void InsertSchemaHistory(OdbcTransaction transaction, AppliedMigration appliedMigration)
    {
        var sql =
            $"INSERT INTO {_configuration.DefaultSchema}.{_configuration.SchemaTable}(" +
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
                $"N'{appliedMigration.Version}'," +
                $"N'{appliedMigration.Description}'," +
                $"N'{appliedMigration.Type}'," +
                $"N'{appliedMigration.Script}'," +
                $"N'{appliedMigration.Checksum}'," +
                $"N'{appliedMigration.InstalledBy}'," +
                "DEFAULT," +
                $"{appliedMigration.ExecutionTime}," +
                $"{appliedMigration.Success})";

        _odbcExecutor.ExecuteNonQuery(transaction, sql);
    }
}