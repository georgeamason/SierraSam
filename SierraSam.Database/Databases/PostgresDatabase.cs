using System.Data;
using System.Data.Odbc;
using SierraSam.Core;

namespace SierraSam.Database.Databases;

public class PostgresDatabase : DefaultDatabase
{
    private readonly Configuration _configuration;

    public PostgresDatabase(OdbcConnection odbcConnection, Configuration configuration)
        : base(odbcConnection, configuration)
    {
        _configuration = configuration
            ?? throw new ArgumentNullException(nameof(configuration));
    }

    public override string Name => "PostgreSQL";

    public override void CreateSchemaHistory(string schema, string table)
    {
        using var command = Connection.CreateCommand();
        command.CommandType = CommandType.Text;
        command.CommandText =
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

        command.ExecuteNonQuery();
    }

    public override void InsertSchemaHistory(OdbcTransaction transaction, Migration migration)
    {
        using var cmd = Connection.CreateCommand();
        cmd.CommandText =
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
                $"{migration.InstalledRank}," +
                $"N'{migration.Version}'," +
                $"N'{migration.Description}'," +
                $"N'{migration.Type}'," +
                $"N'{migration.Script}'," +
                $"N'{migration.Checksum}'," +
                $"N'{migration.InstalledBy}'," +
                "DEFAULT," +
                $"{migration.ExecutionTime}," +
                $"{migration.Success})";
        cmd.CommandType = CommandType.Text;
        cmd.Transaction = transaction;

        cmd.ExecuteNonQuery();
    }
}