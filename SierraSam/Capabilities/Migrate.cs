using System.Collections.Immutable;
using System.Data;
using System.Data.Odbc;
using System.Diagnostics;
using System.IO.Abstractions;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using SierraSam.Core;
using SierraSam.Core.Extensions;
using SierraSam.Database;

namespace SierraSam.Capabilities;

public sealed class Migrate : ICapability
{
    public Migrate
        (ILogger<Migrate> logger,
        IDatabase database,
        Configuration configuration,
        IFileSystem fileSystem)
    {
        _logger = logger
            ?? throw new ArgumentNullException(nameof(logger));

        _database = database
            ?? throw new ArgumentNullException(nameof(database));

        _configuration = configuration
            ?? throw new ArgumentNullException(nameof(configuration));

        _fileSystem = fileSystem
                      ?? throw new ArgumentNullException(nameof(fileSystem));
    }

    public void Run(string[] args)
    {
        _logger.LogInformation($"{nameof(Migrate)} running");

        try
        {
            _database.Connection.Open();
            _logger.LogInformation($"Driver: {_database.Connection.Driver}");
            _logger.LogInformation($"Database: {_database.Connection.Database}");

            Console.WriteLine($"Database: {_database.Connection.Driver}:" +
                              $"{_database.Connection.Database}:" +
                              $"{_database.Connection.ServerVersion}");

            // Create schema table if not found
            if (!_database.HasMigrationTable)
            {
                Console.WriteLine
                    ("Creating Schema History table: " +
                    $"\"{_configuration.DefaultSchema}\".\"{_configuration.SchemaTable}\"");

                _database.CreateSchemaHistory
                    (_configuration.DefaultSchema, _configuration.SchemaTable);
            }

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
                            var migration = new MigrationFile
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

            // Filter out applied migrations
            var appliedMigrations = _database
                .GetSchemaHistory(_configuration.DefaultSchema, _configuration.SchemaTable)
                .ToArray();

            // TODO: There maybe something here about baselines? Need to check what we fetch..
            var pendingMigrations = allMigrations.Where(path =>
            {
                var migration = new MigrationFile
                    (_fileSystem.FileInfo.New(path));

                // TODO: Create a version comparison class - integers have been assumed
                // ReSharper disable once AccessToDisposedClosure
                if (!int.TryParse
                        (appliedMigrations.Max(m => m.Version),
                         out var maxAppliedVersion))
                {
                    _logger.LogInformation($"Schema \"{_configuration.DefaultSchema}\" is clean");
                }

                return int.Parse(migration.Version) > maxAppliedVersion;
            });

            //Console.WriteLine($"Current version of schema \"{_configuration.DefaultSchema}\": ");

            // Apply new migrations
            var installRank = appliedMigrations.MaxBy(m => m.Version)?.InstalledRank ?? 0;
            foreach (var migrationPath in pendingMigrations)
            {
                using var transaction = _database.Connection.BeginTransaction();
                try
                {
                    var migration = new MigrationFile
                        (_fileSystem.FileInfo.New(migrationPath));

                    Console.WriteLine($"Migrating schema \"{_configuration.DefaultSchema}\" " +
                                      $"to version {migration.Version} - {migration.Description}");

                    var migrationSql = _fileSystem.File.ReadAllText(migrationPath);
                    var executionTime = ExecuteMigration(transaction, migrationSql);

                    var pendingMigration = new Migration
                        (++installRank,
                         migration.Version,
                         migration.Description,
                         "SQL",
                         migration.Filename,
                         migrationSql.Checksum(),
                         _configuration.InstalledBy,
                         default,
                         executionTime.TotalMilliseconds,
                         true);

                    // Write to migration history table
                    _database.InsertIntoSchemaHistory(transaction, pendingMigration);

                    transaction.Commit();

                    Console.WriteLine($"Successfully applied 1 migration " +
                                      $"to schema \"{_configuration.DefaultSchema}\" " +
                                      $"(execution time {executionTime:g})");
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, exception.Message);
                    transaction.Rollback();
                    throw;
                }

                installRank++;
            }
        }
        catch (Exception exception)
        {
            var msg = exception switch
            {
                _ => exception.Message
            };

            _logger.LogError(msg, exception);
            throw;
        }
    }

    private TimeSpan ExecuteMigration(OdbcTransaction transaction, string migrationSql)
    {
        using var cmd = _database.Connection.CreateCommand();
        cmd.CommandText = migrationSql;
        cmd.CommandType = CommandType.Text;
        cmd.Transaction = transaction;

        var stopwatch = new Stopwatch();

        stopwatch.Start();
        cmd.ExecuteNonQuery();
        stopwatch.Stop();

        return stopwatch.Elapsed;
    }

    private void InsertIntoMigrationHistoryTable
        (OdbcTransaction transaction,
         int installRank,
         MigrationFile migrationFile,
         string checksum,
         double executionTime)
    {
        using var cmd = _database.Connection.CreateCommand();
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
                {migrationFile.Version},
                N'{migrationFile.Description}',
                N'SQL',
                N'{migrationFile.Filename}',
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

    private readonly IDatabase _database;
    private readonly Configuration _configuration;

    private readonly IFileSystem _fileSystem;
}