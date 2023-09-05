﻿using System.Data;
using System.Data.Odbc;
using System.Diagnostics;
using System.Text;
using SierraSam.Core;

namespace SierraSam.Database;

public abstract class DefaultDatabase : IDatabase
{
    private readonly IConfiguration _configuration;
    private readonly OdbcExecutor _odbcExecutor;

    protected DefaultDatabase(OdbcConnection connection, IConfiguration configuration)
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

    public virtual void CreateSchemaHistory(
        string? schema = null,
        string? table = null,
        OdbcTransaction? transaction = null)
    {
        schema ??= _configuration.DefaultSchema;
        table ??= _configuration.SchemaTable;

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

        _odbcExecutor.ExecuteNonQuery(sql, transaction);
    }

    public virtual IReadOnlyCollection<AppliedMigration> GetSchemaHistory(string? schema = null, string? table = null)
    {
        schema ??= _configuration.DefaultSchema;
        table ??= _configuration.SchemaTable;

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

        if (HasMigrationTable is false)
        {
            throw new InvalidOperationException($"Schema history table " +
                                                $"\"{_configuration.DefaultSchema}\".\"{_configuration.SchemaTable}\" " +
                                                $"does not exist");
        }

        // TODO: These mappings can throw...
        return _odbcExecutor.ExecuteReader<AppliedMigration>(
            sql,
            reader => new AppliedMigration(
                reader.GetInt32("installed_rank"),
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

    public virtual void InsertSchemaHistory(AppliedMigration appliedMigration, OdbcTransaction? transaction = null)
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

        _odbcExecutor.ExecuteNonQuery(sql, transaction);
    }

    public virtual void UpdateSchemaHistory(AppliedMigration appliedMigration, OdbcTransaction? transaction = null)
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

        _odbcExecutor.ExecuteNonQuery(sql, transaction);
    }

    public virtual TimeSpan ExecuteMigration(string sql, OdbcTransaction? transaction = null)
    {
        var stopwatch = new Stopwatch();

        stopwatch.Start();
        _odbcExecutor.ExecuteNonQuery(sql, transaction);
        stopwatch.Stop();

        return stopwatch.Elapsed;
    }

    public virtual IReadOnlyCollection<DatabaseObject> GetSchemaObjects(
        string? schema = null,
        OdbcTransaction? transaction = null)
    {
        schema ??= _configuration.DefaultSchema;

        //  TODO: How about Triggers - they are not in sys.objects
        var sql = "SELECT o.name, o.type, t.name AS parent" + Environment.NewLine +
                  "FROM sys.objects o" + Environment.NewLine +
                  "INNER JOIN sys.schemas s ON s.[schema_id] = o.[schema_id]" + Environment.NewLine +
                  "LEFT JOIN sys.tables t ON t.[object_id] = o.parent_object_id" + Environment.NewLine +
                  $"WHERE s.name = '{schema}'" + Environment.NewLine +
                  "AND o.is_ms_shipped = 0" + Environment.NewLine +
                  "ORDER BY o.parent_object_id DESC, o.[object_id] DESC";

        return _odbcExecutor.ExecuteReader(
            sql,
            reader => new DatabaseObject(
                schema,
                reader.GetString("name").Trim(),
                reader.GetString("type").Trim(),
                reader["parent"] as string),
            transaction);
    }

    public virtual void DropSchemaObject(DatabaseObject obj, OdbcTransaction? transaction = null)
    {
        var objectType = obj.Type switch
        {
            "AF" => "AGGREGATE",
            "C" or "D" or "F" or "PK" or "UQ" => "CONSTRAINT",
            "FN" or "IF" or "TF" => "FUNCTION",
            "P" => "PROCEDURE",
            "U" => "TABLE",
            "V" => "VIEW",
            _   => throw new ArgumentOutOfRangeException(nameof(obj), $"Unknown database object type '{obj.Type}'")
        };

        var sb = new StringBuilder();

        if (obj.Parent is not null)
            sb.Append($"ALTER TABLE \"{obj.Schema}\".\"{obj.Parent}\"");

        sb.Append($"DROP {objectType} \"{obj.Name}\"");

        _odbcExecutor.ExecuteNonQuery(sb.ToString(), transaction);
    }
}