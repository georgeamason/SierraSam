using System.Data;
using System.Data.Odbc;
using SierraSam.Core;
using SierraSam.Core.Extensions;

namespace SierraSam.Database;

public abstract class Database : IDatabase
{
    private readonly Configuration _configuration;

    protected Database(OdbcConnection connection, Configuration configuration)
    {
        Connection = connection
                     ?? throw new ArgumentNullException(nameof(connection));

        _configuration = configuration
            ?? throw new ArgumentNullException(nameof(configuration));
    }

    public abstract string Name { get; }
    public OdbcConnection Connection { get; }

    public virtual bool HasMigrationTable
    {
        get
        {
            var dataTable = Connection.GetSchema("Tables");

            return dataTable.HasMigrationHistory(_configuration);
        }
    }

    public virtual void CreateSchemaHistory(string schema, string table)
    {
        using var command = Connection.CreateCommand();
        command.CommandType = CommandType.Text;
        command.CommandText =
            $"CREATE TABLE {schema}.{table}(" +
             "[installed_rank] INT PRIMARY KEY NOT NULL," +
             "[version] NVARCHAR(50) NULL," +
             "[description] NVARCHAR(200) NULL," +
             "[type] NVARCHAR(20) NOT NULL," +
             "[script] NVARCHAR(1000) NOT NULL," +
             "[checksum] NVARCHAR(32) NULL," +
             "[installed_by] NVARCHAR(100) NOT NULL," +
             "[installed_on] DATETIME NOT NULL DEFAULT (GETDATE())," +
             "[execution_time] FLOAT NOT NULL," +
             "[success] BIT NOT NULL)";

        command.ExecuteNonQuery();
    }

    public virtual IEnumerable<Migration> GetSchemaHistory(string schema, string table)
    {
        using var cmd = Connection.CreateCommand();
        cmd.CommandType = CommandType.Text;
        cmd.CommandText = 
            "SELECT \"installed_rank\"," +
            "\"version\"," +
            "\"description\"," +
            "\"type\"," +
            "\"script\"," +
            "\"checksum\"," +
            "\"installed_by\"," +
            "\"installed_on\"," +
            "\"execution_time\"," +
            "\"success\" " +
           $"FROM \"{schema}\".\"{table}\" " +
            "ORDER BY \"installed_rank\"";

        using var dataReader = cmd.ExecuteReader();

        if (!dataReader.HasRows) yield break;

        while (dataReader.Read())
        {
            yield return new Migration
                (dataReader.GetInt32("installed_rank"),
                 dataReader.GetString("version"),
                 dataReader.GetString("description"),
                 dataReader.GetString("type"),
                 dataReader.GetString("script"),
                 dataReader.GetString("checksum"),
                 dataReader.GetString("installed_by"),
                 dataReader.GetDateTime("installed_on"),
                 dataReader.GetDouble("execution_time"),
                 dataReader.GetBoolean("success"));
        }
    }

    public virtual void InsertIntoSchemaHistory(OdbcTransaction transaction, Migration migration)
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
                $"{(migration.Success ? 1 : 0)})";
        cmd.CommandType = CommandType.Text;
        cmd.Transaction = transaction;

        cmd.ExecuteNonQuery();
    }
}