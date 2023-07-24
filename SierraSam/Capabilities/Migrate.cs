using System.IO.Abstractions;
using Microsoft.Extensions.Logging;
using SierraSam.Core;
using SierraSam.Core.Extensions;
using SierraSam.Core.MigrationSeekers;
using SierraSam.Database;

namespace SierraSam.Capabilities;

public sealed class Migrate : ICapability
{
    public Migrate(ILogger<Migrate> logger,
                   IDatabase database,
                   Configuration configuration,
                   IFileSystem fileSystem,
                   IMigrationSeeker migrationSeeker)
    {
        _logger = logger
            ?? throw new ArgumentNullException(nameof(logger));

        _database = database
            ?? throw new ArgumentNullException(nameof(database));

        _configuration = configuration
            ?? throw new ArgumentNullException(nameof(configuration));

        _fileSystem = fileSystem
            ?? throw new ArgumentNullException(nameof(fileSystem));

        _migrationSeeker = migrationSeeker
            ?? throw new ArgumentNullException(nameof(migrationSeeker));
    }

    public void Run(string[] args)
    {
        _logger.LogTrace($"{nameof(Migrate)} running");

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

            // Search file system for migrations
            var allMigrations = _migrationSeeker.Find();

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
                if (!int.TryParse(appliedMigrations.Max(m => m.Version), out var maxAppliedVersion))
                {
                    _logger.LogInformation($"Schema \"{_configuration.DefaultSchema}\" is clean");
                }

                return int.Parse(migration.Version!) > maxAppliedVersion;
            });

            // Console.WriteLine($"Current version of schema \"{_configuration.DefaultSchema}\": {appliedMigrations.Max(m => m.Version)}");

            // Apply new migrations
            var installRank = appliedMigrations.MaxBy(m => m.Version)?.InstalledRank ?? 0;
            foreach (var pendingMigration in pendingMigrations)
            {
                using var transaction = _database.Connection.BeginTransaction();
                try
                {
                    var migrationFile = new MigrationFile
                        (_fileSystem.FileInfo.New(pendingMigration));

                    Console.WriteLine($"Migrating schema \"{_configuration.DefaultSchema}\" " +
                                      $"to version {migrationFile.Version} - {migrationFile.Description}");

                    var migrationSql = _fileSystem.File.ReadAllText(pendingMigration);

                    var executionTime = _database.ExecuteMigration(transaction, migrationSql);

                    // Write to migration history table
                    var migration = new Migration(
                        ++installRank,
                        migrationFile.Version!,
                        migrationFile.Description,
                        "SQL",
                        migrationFile.Filename,
                        migrationSql.Checksum(),
                        _configuration.InstalledBy,
                        default,
                        executionTime.TotalMilliseconds,
                        true);

                    _database.InsertSchemaHistory(transaction, migration);

                    transaction.Commit();

                    Console.WriteLine($"Successfully applied 1 migration " +
                                      $"to schema \"{_configuration.DefaultSchema}\" " +
                                      $"(execution time {executionTime:g})");
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "Unable to apply migration");
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

    private readonly ILogger _logger;

    private readonly IDatabase _database;

    private readonly Configuration _configuration;

    private readonly IFileSystem _fileSystem;

    private readonly IMigrationSeeker _migrationSeeker;
}