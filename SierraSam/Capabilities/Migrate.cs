using System.Collections.Immutable;
using System.Data;
using Microsoft.Extensions.Logging;
using SierraSam.Core;
using SierraSam.Core.MigrationSeekers;
using static SierraSam.Core.Enums.MigrationType;
using Console = SierraSam.Core.ColorConsole;

namespace SierraSam.Capabilities;

internal sealed class Migrate : ICapability
{
    private readonly ILogger _logger;
    private readonly IDatabase _database;
    private readonly IConfiguration _configuration;
    private readonly IMigrationSeeker _migrationSeeker;
    private readonly IMigrationApplicator _migrationApplicator;

    public Migrate(ILogger<Migrate> logger,
                   IDatabase database,
                   IConfiguration configuration,
                   IMigrationSeeker migrationSeeker,
                   IMigrationApplicator migrationApplicator)
    {
        _logger = logger
            ?? throw new ArgumentNullException(nameof(logger));

        _database = database
            ?? throw new ArgumentNullException(nameof(database));

        _configuration = configuration
            ?? throw new ArgumentNullException(nameof(configuration));

        _migrationSeeker = migrationSeeker
            ?? throw new ArgumentNullException(nameof(migrationSeeker));

        _migrationApplicator = migrationApplicator
            ?? throw new ArgumentNullException(nameof(migrationApplicator));
    }

    public void Run(string[] args)
    {
        _logger.LogTrace($"{nameof(Migrate)} running");

        if (_database.Connection.State is not ConnectionState.Open) _database.Connection.Open();

        _logger.LogInformation("Driver: {Driver}", _database.Connection.Driver);
        _logger.LogInformation("Database: {Database}", _database.Connection.Database);

        Console.WriteLine($"Database: {_database.Connection.Driver}:" +
                          $"{_database.Connection.Database}:" +
                          $"{_database.Connection.ServerVersion}");

        if (!_database.HasMigrationTable)
        {
            Console.WriteLine
                ("Creating schema history table: " +
                $"\"{_configuration.DefaultSchema}\".\"{_configuration.SchemaTable}\"");

            // TODO: How about if the default schema has not been created?
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
            .Where(pendingMigration =>
            {
                return pendingMigration.MigrationType is Repeatable ||
                       new VersionComparator(pendingMigration.Version!)
                           .IsGreaterThan(appliedMigrations.Max(x => x.Version) ?? "0");
            })
            .OrderBy(pendingMigration => pendingMigration.MigrationType)
            .ThenBy(pendingMigration => pendingMigration.Version)
            .ThenBy(pendingMigration => pendingMigration.Description)
            .ToImmutableArray();

        var (appliedMigrationCount, executionTime) =
            _migrationApplicator.Apply(pendingMigrations, appliedMigrations);

        if (appliedMigrationCount == 0)
        {
            Console.SuccessLine($"Schema \"{_configuration.DefaultSchema}\" is up to date");

            return;
        }

        Console.SuccessLine($"Successfully applied {appliedMigrationCount} migration(s) " +
                            $"to schema \"{_configuration.DefaultSchema}\" " +
                            $@"(execution time {executionTime:mm\:ss\.fff}s)");
    }
}