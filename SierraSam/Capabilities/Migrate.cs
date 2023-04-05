using System.Data;
using System.Data.Odbc;
using System.Diagnostics;
using System.IO.Abstractions;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using SierraSam.Core;
using SierraSam.Core.Extensions;

namespace SierraSam.Capabilities;

public sealed class Migrate : ICapability
{
    public Migrate
        (ILogger<Migrate> logger,
         OdbcConnection odbcConnection,
         Configuration configuration,
         IFileSystem fileSystem)
    {
        _logger = logger
            ?? throw new ArgumentNullException(nameof(logger));

        _odbcConnection = odbcConnection
            ?? throw new ArgumentNullException(nameof(odbcConnection));

        _configuration = configuration
            ?? throw new ArgumentNullException(nameof(configuration));

        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
    }

    public void Run(string[] args)
    {
        _logger.LogInformation($"{nameof(Migrate)} running");

        try
        {
            _odbcConnection.Open();
            _logger.LogInformation($"Driver: {_odbcConnection.Driver}");
            _logger.LogInformation($"Database: {_odbcConnection.Database}");

            Console.WriteLine($"Database: {_odbcConnection.Driver}:" +
                              $"{_odbcConnection.Database}:" +
                              $"{_odbcConnection.ServerVersion}");

            var dbTables = _odbcConnection.GetSchema("Tables");

            // Create schema table if not found
            if (!dbTables.HasMigrationHistory(_configuration)) 
                CreateMigrationHistoryTable();

            // TODO: Search file system for migrations
            // Will need to abstract this as well as calling s3 buckets etc
            // Directory needs to be injected
            var allMigrations = _configuration.Locations
                .Where(d => d.StartsWith("filesystem:"))
                .SelectMany(d =>
                {
                    var path = d.Split(':', 2).Last();

                    return _fileSystem.Directory.GetFiles
                        (path, "*", SearchOption.AllDirectories)
                        .Where(migrationPath =>
                        {
                            var migration = new Migration
                                (_fileSystem.FileInfo.New(migrationPath));

                            // V1__My_description.sql
                            // V1.1__My_description.sql
                            // V1.1.1.1.1.__My_description.sql
                            return Regex.IsMatch
                                ($"{migration.Filename}",
                                 $"{_configuration.MigrationPrefix}\\d+(\\.?\\d{{0,}})+" +
                                 $"{_configuration.MigrationSeparator}\\w+" +
                                 $"({string.Join('|', _configuration.MigrationSuffixes)})");
                        });
                });

            // TODO: Filter out applied migrations
            using var appliedMigrations = GetMigrationHistory();

            // TODO: There maybe something here about baselines? Need to check what we fetch..
            var pendingMigrations = allMigrations.Where(path =>
            {
                var migration = new Migration
                    (_fileSystem.FileInfo.New(path));

                // TODO: Create a version comparison class - integers have been assumed
                // ReSharper disable once AccessToDisposedClosure
                int.TryParse(appliedMigrations?.Rows[^1].Field<string>("version"),
                             out var maxAppliedVersion);

                return int.Parse(migration.Version) > maxAppliedVersion;
            });

            //Console.WriteLine($"Current version of schema \"{_configuration.DefaultSchema}\": ");

            // TODO: Apply new migrations
            foreach (var migrationPath in pendingMigrations)
            {
                using var transaction = _odbcConnection.BeginTransaction();
                try
                {
                    var migration = new Migration
                        (_fileSystem.FileInfo.New(migrationPath));

                    Console.WriteLine($"Migrating schema \"{_configuration.DefaultSchema}\" " +
                                      $"to version {migration.Version} - {migration.Description}");

                    var migrationSql = File.ReadAllText(migrationPath);
                    var executionTime = ExecuteMigration(transaction, migrationSql);

                    // TODO: Write to migration history table
                    var installRank =
                        appliedMigrations?.Rows[^1].Field<int>("installed_rank") + 1 ?? 1;

                    InsertIntoMigrationsHistoryTable
                        (transaction,
                         installRank,
                         migration,
                         migrationSql.Checksum(),
                         executionTime.TotalMilliseconds);

                    transaction.Commit();
                    Console.WriteLine(
                        $"Successfully applied 1 migration to schema \"{_configuration.DefaultSchema}\" (execution time {executionTime:g})");
                }
                catch (Exception exception)
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }
        catch (Exception exception)
        {
            var msg = exception switch
            {
                _ => exception.Message
            };

            _logger.LogError(msg, exception);
        }
    }

    private void CreateMigrationHistoryTable()
    {
        Console.WriteLine($"Creating Schema History table:" +
                          $"\"{_configuration.DefaultSchema}\".\"{_configuration.SchemaTable}\"");

        using var command = _odbcConnection.CreateCommand();
        command.CommandType = CommandType.Text;
        command.CommandText =
            @$"CREATE TABLE [{_configuration.DefaultSchema}].[{_configuration.SchemaTable}]
               (
                   [installed_rank] [INT] PRIMARY KEY NOT NULL,
                   [version] [NVARCHAR](50) NULL,
                   [description] [NVARCHAR](200) NULL,
                   [type] [NVARCHAR](20) NOT NULL,
                   [script] [NVARCHAR](1000) NOT NULL,
                   [checksum] [NVARCHAR] (32) NULL,
                   [installed_by] [NVARCHAR](100) NOT NULL,
                   [installed_on] [DATETIME] NOT NULL DEFAULT (GETDATE()),
                   [execution_time] [FLOAT] NOT NULL,
                   [success] [BIT] NOT NULL
                )";

        command.ExecuteNonQuery();
    }

    private DataTable? GetMigrationHistory()
    {
        using var cmd = _odbcConnection.CreateCommand();
        cmd.CommandType = CommandType.Text;
        cmd.CommandText = 
            @$"SELECT installed_rank, version
               FROM [{_configuration.DefaultSchema}].[{_configuration.SchemaTable}]";

        using var dataReader = cmd.ExecuteReader();

        return dataReader.GetData();
    }

    private TimeSpan ExecuteMigration(OdbcTransaction transaction, string migrationSql)
    {
        using var cmd = _odbcConnection.CreateCommand();
        cmd.CommandText = migrationSql;
        cmd.CommandType = CommandType.Text;
        cmd.Transaction = transaction;

        var stopwatch = new Stopwatch();

        stopwatch.Start();
        cmd.ExecuteNonQuery();
        stopwatch.Stop();

        return stopwatch.Elapsed;
    }

    private void InsertIntoMigrationsHistoryTable
        (OdbcTransaction transaction,
         int installRank,
         Migration migration,
         string checksum,
         double executionTime)
    {
        using var cmd = _odbcConnection.CreateCommand();
        cmd.CommandText =
            @$"INSERT INTO [{_configuration.DefaultSchema}].[{_configuration.SchemaTable}]
            (
                installed_rank,
                version,
                description,
                type,
                script,
                checksum,
                installed_by,
                installed_on,
                execution_time,
                success
            )
            VALUES
            (   {installRank},
                {migration.Version},
                N'{migration.Description}',
                N'SQL',
                N'{migration.Filename}',
                N'{checksum}',
                N'{_configuration.InstalledBy}',
                DEFAULT,
                {executionTime},
                N'1'
            )";
        cmd.CommandType = CommandType.Text;
        cmd.Transaction = transaction;

        cmd.ExecuteNonQuery();
    }

    private readonly ILogger _logger;

    private readonly OdbcConnection _odbcConnection;

    private readonly Configuration _configuration;

    private readonly IFileSystem _fileSystem;
}