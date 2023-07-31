using System.Data.Odbc;
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

        var discoveredMigrations = _migrationSeeker.Find();

        var appliedMigrations = _database.GetSchemaHistory
            (_configuration.DefaultSchema, _configuration.SchemaTable);

        Console.WriteLine($"Current version of schema \"{_configuration.DefaultSchema}\":" +
                          $" {appliedMigrations.Max(x => x.Version) ?? "<< Empty Schema >>"}");

        // TODO: There maybe something here about baselines? Need to check what we fetch..
        var pendingMigrations = discoveredMigrations
            .Select(path => _fileSystem.FileInfo.New(path))
            .Select(fileInfo => PendingMigration.Parse(_configuration, fileInfo))
            .Where(pendingMigration =>
            {

               if (pendingMigration.MigrationType is MigrationType.Repeatable) return true;

                return VersionComparator.Compare
                    (pendingMigration.Version!,
                     appliedMigrations.Max(x => x.Version)!);
            })
            .OrderBy(pendingMigration => pendingMigration.Version)
            .ThenBy(pendingMigration => pendingMigration.Description)
            .ToArray();

        // Apply migrations
        var installRank = appliedMigrations
            .Select(m => m.InstalledRank)
            .DefaultIfEmpty(0)
            .Max();

        using var transaction = _database.Connection.BeginTransaction();
        var appliedMigrationCount = 0;
        var executionTime = TimeSpan.Zero;
        foreach (var pendingMigration in pendingMigrations)
        {
            try
            {
                var migrationSql = _fileSystem.File.ReadAllText(pendingMigration.FilePath);

                if (pendingMigration.MigrationType is MigrationType.Versioned)
                {
                    Console.WriteLine($"Migrating schema \"{_configuration.DefaultSchema}\" " +
                                      $"to version {pendingMigration.Version} - {pendingMigration.Description}");
                }

                var checksum = migrationSql.Checksum();
                if (pendingMigration.MigrationType is MigrationType.Repeatable)
                {
                    if (appliedMigrations.Any(m => m.Checksum == checksum)) continue;

                    Console.WriteLine($"Applying repeatable migration - {pendingMigration.Description}");
                }

                executionTime += _database.ExecuteMigration(transaction, migrationSql);

                var migration = new AppliedMigration(
                    ++installRank,
                    pendingMigration.Version,
                    pendingMigration.Description,
                    "SQL",
                    pendingMigration.FileName,
                    checksum,
                    _configuration.InstalledBy,
                    default,
                    executionTime.TotalMilliseconds,
                    true);

                _database.InsertSchemaHistory(transaction, migration);

                appliedMigrationCount++;
            }
            catch (OdbcException exception)
            {
                transaction.Rollback();

                throw new Exception
                    ($"Failed to apply migration \"{pendingMigration}\"; rolled back the transaction.",
                     exception);
            }
        }

        transaction.Commit();

        if (appliedMigrationCount > 0)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Successfully applied {appliedMigrationCount} migration(s) " +
                              $"to schema \"{_configuration.DefaultSchema}\" " +
                              $"(execution time {executionTime:mm\\:ss\\.fff}s)");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Schema \"{_configuration.DefaultSchema}\" is up to date");
            Console.ResetColor();
        }
    }
}