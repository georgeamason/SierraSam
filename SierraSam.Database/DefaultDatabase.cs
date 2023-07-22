using System.Data;
using System.Data.Odbc;
using System.Diagnostics;
using SierraSam.Core;
using SierraSam.Core.Extensions;

namespace SierraSam.Database;

public abstract class DefaultDatabase : IDatabase
{
    private readonly Configuration _configuration;

    private readonly OdbcExecutor _odbcExecutor;

    protected DefaultDatabase(OdbcConnection connection, Configuration configuration)
    {
        Connection = connection
                     ?? throw new ArgumentNullException(nameof(connection));

        _configuration = configuration
            ?? throw new ArgumentNullException(nameof(configuration));

        _odbcExecutor = new OdbcExecutor(connection);
    }

    public abstract string Name { get; }
    public OdbcConnection Connection { get; }

    public virtual bool HasMigrationTable => HasTable(_configuration.SchemaTable);

    public virtual bool HasTable(string tableName)
    {
        var dataTable = Connection.GetSchema("Tables");

        foreach (DataRow row in dataTable.Rows)
        {
            // https://learn.microsoft.com/en-us/dotnet/framework/data/adonet/odbc-schema-collections
            var dbTableName = row["TABLE_NAME"] as string;

            if (dbTableName == tableName)
                return true;
        }

        return false;
    }

    public virtual void CreateSchemaHistory(string schema, string table)
    {
        using var command = Connection.CreateCommand();
        command.CommandType = CommandType.Text;
        command.CommandText =
            $"CREATE TABLE {schema}.{table}(" +
             "\"installed_rank\" INT PRIMARY KEY NOT NULL," +
             "\"version\" NVARCHAR(50) NULL," +
             "\"description\" NVARCHAR(200) NULL," +
             "\"type\" NVARCHAR(20) NOT NULL," +
             "\"script\" NVARCHAR(1000) NOT NULL," +
             "\"checksum\" NVARCHAR(32) NULL," +
             "\"installed_by\" NVARCHAR(100) NOT NULL," +
             "\"installed_on\" DATETIME NOT NULL DEFAULT (GETDATE())," +
             "\"execution_time\" FLOAT NOT NULL," +
             "\"success\" BIT NOT NULL)";

        command.ExecuteNonQuery();
    }

    public virtual IEnumerable<Migration> GetSchemaHistory(string schema, string table)
    {
        var sql = "SELECT \"installed_rank\"," +
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

        return _odbcExecutor.ExecuteReader<Migration>
            (sql,
             reader => new Migration
                (reader.GetInt32("installed_rank"),
                 reader.GetString("version"),
                 reader.GetString("description"),
                 reader.GetString("type"),
                 reader.GetString("script"),
                 reader.GetString("checksum"),
                 reader.GetString("installed_by"),
                 reader.GetDateTime("installed_on"),
                 reader.GetDouble("execution_time"),
                 reader.GetBoolean("success"))
             );
    }

    public virtual void InsertSchemaHistory(OdbcTransaction transaction, Migration migration)
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

    public virtual TimeSpan ExecuteMigration(OdbcTransaction transaction, string sql)
    {
        using var cmd = Connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.CommandType = CommandType.Text;
        cmd.Transaction = transaction;

        var stopwatch = new Stopwatch();

        stopwatch.Start();
        cmd.ExecuteNonQuery();
        stopwatch.Stop();

        return stopwatch.Elapsed;
    }
}