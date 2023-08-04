using System.Data;
using System.Data.Odbc;
using System.Diagnostics;
using SierraSam.Core;

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

            if (dbTableName == tableName) return true;
        }

        return false;
    }

    public virtual void CreateSchemaHistory(string schema, string table)
    {
        var sql =
            $"CREATE TABLE {schema}.{table} (" +
             "\"installed_rank\" INT PRIMARY KEY NOT NULL," +
             "\"version\" NVARCHAR(50) NULL," +
             "\"description\" NVARCHAR(200) NOT NULL," +
             "\"type\" NVARCHAR(20) NOT NULL," +
             "\"script\" NVARCHAR(1000) NOT NULL," +
             "\"checksum\" NVARCHAR(32) NOT NULL," +
             "\"installed_by\" NVARCHAR(100) NOT NULL," +
             "\"installed_on\" DATETIME NOT NULL DEFAULT (GETDATE())," +
             "\"execution_time\" FLOAT NOT NULL," +
             "\"success\" BIT NOT NULL)";

        _odbcExecutor.ExecuteNonQuery(sql);
    }

    public virtual IReadOnlyCollection<AppliedMigration> GetSchemaHistory(string schema, string table)
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

        // TODO: These mappings can throw...
        return _odbcExecutor.ExecuteReader<AppliedMigration>
            (sql,
             reader => new AppliedMigration
                (reader.GetInt32("installed_rank"),
                 reader["version"] as string,
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

    public virtual void InsertSchemaHistory(OdbcTransaction transaction, AppliedMigration appliedMigration)
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
                (appliedMigration.Version is not null ? $"N'{appliedMigration.Version}'," : $"NULL,") +
                $"N'{appliedMigration.Description}'," +
                $"N'{appliedMigration.Type}'," +
                $"N'{appliedMigration.Script}'," +
                $"N'{appliedMigration.Checksum}'," +
                $"N'{appliedMigration.InstalledBy}'," +
                 "DEFAULT," +
                $"{appliedMigration.ExecutionTime}," +
                $"{(appliedMigration.Success ? 1 : 0)})";

        _odbcExecutor.ExecuteNonQuery(transaction, sql);
    }

    public virtual void UpdateSchemaHistory(OdbcTransaction transaction, AppliedMigration appliedMigration)
    {
        var sql =
            $"UPDATE {_configuration.DefaultSchema}.{_configuration.SchemaTable}" + Environment.NewLine +
            $"SET \"version\" = {(appliedMigration.Version is not null ? $"N'{appliedMigration.Version}'," : "NULL,")}" + Environment.NewLine +
                $"\"description\" = N'{appliedMigration.Description}'," + Environment.NewLine +
                $"\"type\" = N'{appliedMigration.Type}'," + Environment.NewLine +
                $"\"script\" = N'{appliedMigration.Script}'," + Environment.NewLine +
                $"\"checksum\" = N'{appliedMigration.Checksum}'," + Environment.NewLine +
                $"\"installed_by\"   = N'{appliedMigration.InstalledBy}'," + Environment.NewLine +
                $"\"installed_on\"   = N'{appliedMigration.InstalledOn:yyyy-MM-dd HH:mm:ss}'," + Environment.NewLine +
                $"\"execution_time\" = N'{appliedMigration.ExecutionTime}'" + Environment.NewLine +
                // $"\"success\"        = N'{appliedMigration.Success}'" + Environment.NewLine +
            $"WHERE installed_rank = {appliedMigration.InstalledRank};";

        _odbcExecutor.ExecuteNonQuery(transaction, sql);
    }

    public virtual TimeSpan ExecuteMigration(OdbcTransaction transaction, string sql)
    {
        var stopwatch = new Stopwatch();

        stopwatch.Start();
        _odbcExecutor.ExecuteNonQuery(transaction, sql);
        stopwatch.Stop();

        return stopwatch.Elapsed;
    }
}