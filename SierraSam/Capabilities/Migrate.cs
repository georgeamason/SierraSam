using System.IO.Abstractions;
using Microsoft.Extensions.Logging;
using SierraSam.Core;
using SierraSam.Core.Extensions;
using SierraSam.Core.MigrationSeekers;
using SierraSam.Database;

namespace SierraSam.Capabilities;

public sealed class Migrate : ICapability
{
    private readonly ILogger _logger;

    private readonly IDatabase _database;

    private readonly Configuration _configuration;

    private readonly IFileSystem _fileSystem;

    private readonly IMigrationSeeker _migrationSeeker;

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

            _logger.LogInformation("Driver: {Driver}", _database.Connection.Driver);
            _logger.LogInformation("Database: {Database}", _database.Connection.Database);

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
            var appliedMigrations = _database.GetSchemaHistory
                (_configuration.DefaultSchema, _configuration.SchemaTable);

            // TODO: There maybe something here about baselines? Need to check what we fetch..
            // Filter all migrations by applied migrations. Filtering key is version.
            var pendingMigrations = allMigrations.Where(path =>
            {
                // TODO: Would like to sort this out?
                var m = new MigrationFile
                    (_fileSystem.FileInfo.New(path));

                Console.WriteLine($"Current version of schema \"{_configuration.DefaultSchema}\":" +
                                  $" {appliedMigrations.Max(x => x.Version) ?? "<< Empty Schema >>"}");

                // ReSharper disable once InvertIf
                if (!appliedMigrations.Any())
                {
                    _logger.LogInformation
                        ("Schema \"{DefaultSchema}\" is clean", _configuration.DefaultSchema);

                    return true;
                }

                // TODO:  m.Version could be null here for repeatable migrations
                return VersionComparator.Compare(m.Version, appliedMigrations.Max(x => x.Version)!);
            });


            // Apply new migrations
            var installRank = appliedMigrations.MaxBy(m => m.Version)?.InstalledRank ?? 1;
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
                        installRank,
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
                                      $"(execution time {executionTime:mm\\:ss\\.fff}s)");
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
            _logger.LogError(exception, "A problem occured with the migration");
            throw;
        }
    }
}