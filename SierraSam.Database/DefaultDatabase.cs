using System.Data;
using System.Data.Odbc;
using System.Diagnostics;
using System.Text;
using SierraSam.Core;

namespace SierraSam.Database;

public abstract class DefaultDatabase : IDatabase
{
    private readonly IConfiguration _configuration;
    private readonly OdbcExecutor _odbcExecutor;

    protected DefaultDatabase(IDbConnection connection, IConfiguration configuration)
    {
        Connection = connection
            ?? throw new ArgumentNullException(nameof(connection));

        _configuration = configuration
            ?? throw new ArgumentNullException(nameof(configuration));

        _odbcExecutor = new OdbcExecutor(connection);
    }

    public abstract string Name { get; }
    public IDbConnection Connection { get; }

    public virtual bool HasMigrationTable => HasTable(_configuration.SchemaTable);

    public virtual bool HasTable(string tableName)
    {
        var sql = $"SELECT \"TABLE_NAME\" " +
                  $"FROM INFORMATION_SCHEMA.TABLES " +
                  $"WHERE \"TABLE_NAME\" = \"{tableName}\"";

        var result = _odbcExecutor.ExecuteReader<string>(
            sql,
            reader => reader.GetString(0)
        );

        return result.Any();
    }

    public virtual void CreateSchemaHistory(
        string? schema = null,
        string? table = null,
        IDbTransaction? transaction = null)
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
             "\"installed_on\" DATETIME NOT NULL DEFAULT (GETUTCDATE())," +
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
                reader.GetBoolean(9))
        );
    }

    public virtual void InsertSchemaHistory(AppliedMigration appliedMigration, IDbTransaction? transaction = null)
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

    public virtual void UpdateSchemaHistory(AppliedMigration appliedMigration, IDbTransaction? transaction = null)
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

    public virtual TimeSpan ExecuteMigration(string sql, IDbTransaction? transaction = null)
    {
        var stopwatch = new Stopwatch();

        stopwatch.Start();
        _odbcExecutor.ExecuteNonQuery(sql, transaction);
        stopwatch.Stop();

        return stopwatch.Elapsed;
    }

    public virtual IReadOnlyCollection<DatabaseObject> GetSchemaObjects(
        string? schema = null,
        IDbTransaction? transaction = null)
    {
        schema ??= _configuration.DefaultSchema;

        //  TODO: How about Triggers - they are not in sys.objects
        var sql = $"SELECT o.name, o.type, t.name AS parent " +
                  $"FROM sys.objects o " +
                  $"INNER JOIN sys.schemas s ON s.[schema_id] = o.[schema_id] " +
                  $"LEFT JOIN sys.tables t ON t.[object_id] = o.parent_object_id " +
                  $"WHERE s.name = '{schema}' AND o.is_ms_shipped = 0 " +
                  $"ORDER BY o.parent_object_id DESC, o.[object_id] DESC";

        return _odbcExecutor.ExecuteReader<DatabaseObject>(
            sql,
            reader => new DatabaseObject(
                schema,
                reader.GetString(0).Trim(),
                !reader.IsDBNull(1) ? reader.GetString(1).Trim() : null,
                !reader.IsDBNull(2) ? reader.GetString(2).Trim() : null),
            transaction);
    }

    public virtual void DropSchemaObject(DatabaseObject obj, IDbTransaction? transaction = null)
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