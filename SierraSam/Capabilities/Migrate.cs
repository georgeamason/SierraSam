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

        // TODO: There maybe something here about baselines? Need to check what we fetch..
        var pendingMigrations = discoveredMigrations
            .Select(path => _fileSystem.FileInfo.New(path))
            .Where(fileInfo => fileInfo.Name.StartsWith(_configuration.MigrationPrefix) ||
                               fileInfo.Name.StartsWith(_configuration.UndoMigrationPrefix))
            .Select(fileInfo => PendingMigration.Parse(_configuration, fileInfo))
            .Where(versionedMigration =>
            {
                Console.WriteLine($"Current version of schema \"{_configuration.DefaultSchema}\":" +
                                  $" {appliedMigrations.Max(x => x.Version) ?? "<< Empty Schema >>"}");

                // ReSharper disable once InvertIf
                if (!appliedMigrations.Any())
                {
                    _logger.LogInformation
                        ("Schema \"{DefaultSchema}\" is clean", _configuration.DefaultSchema);

                    return true;
                }

                return VersionComparator.Compare
                    (versionedMigration.Version!,
                     appliedMigrations.Max(x => x.Version)!);
            });

        // Apply versioned migrations
        var installRank = appliedMigrations.MaxBy(m => m.Version)?.InstalledRank ?? 1;
        foreach (var pendingMigration in pendingMigrations)
        {
            using var transaction = _database.Connection.BeginTransaction();
            try
            {
                Console.WriteLine($"Migrating schema \"{_configuration.DefaultSchema}\" " +
                                  $"to version {pendingMigration.Version} - {pendingMigration.Description}");

                var migrationSql = _fileSystem.File.ReadAllText(pendingMigration.FilePath);

                var executionTime = _database.ExecuteMigration(transaction, migrationSql);

                var migration = new AppliedMigration(
                    installRank,
                    pendingMigration.Version,
                    pendingMigration.Description,
                    "SQL",
                    pendingMigration.FileName,
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
            catch (OdbcException exception)
            {
                transaction.Rollback();
                throw new Exception($"Unable to apply versioned migration \"{pendingMigration}\"", exception);
            }

            installRank++;
        }

        // Apply repeatable migrations
        var repeatableMigrations = discoveredMigrations
            .Select(path => _fileSystem.FileInfo.New(path))
            .Where(fileInfo => fileInfo.Name.StartsWith(_configuration.RepeatableMigrationPrefix))
            .OrderBy(fileInfo => fileInfo.Name);

        foreach (var repeatableMigration in repeatableMigrations)
        {
            try
            {
                var migrationSql = _fileSystem.File.ReadAllText(repeatableMigration.FullName);

                _database.ExecuteMigration(migrationSql);
            }
            catch (OdbcException exception)
            {
                throw new Exception
                    ($"Unable to apply repeatable migration \"{repeatableMigration.FullName}\"",
                     exception);
            }
        }
    }
}