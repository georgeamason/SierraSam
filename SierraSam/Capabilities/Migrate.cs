using Microsoft.Extensions.Logging;
using SierraSam.Core;
using SierraSam.Core.MigrationSeekers;
using SierraSam.Core.MigrationValidators;
using Spectre.Console;
using static SierraSam.Core.Enums.MigrationType;

namespace SierraSam.Capabilities;

internal sealed class Migrate : ICapability
{
    private readonly ILogger _logger;
    private readonly IDatabase _database;
    private readonly IConfiguration _configuration;
    private readonly IMigrationValidator _validator;
    private readonly IMigrationSeeker _migrationSeeker;
    private readonly IMigrationsApplicator _migrationsApplicator;
    private readonly IAnsiConsole _console;

    // ReSharper disable once ConvertToPrimaryConstructor
    public Migrate(
        ILogger<Migrate> logger,
        IDatabase database,
        IConfiguration configuration,
        IMigrationValidator validator,
        IMigrationSeeker migrationSeeker,
        IMigrationsApplicator migrationsApplicator,
        IAnsiConsole console
    )
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _database = database ?? throw new ArgumentNullException(nameof(database));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _migrationSeeker = migrationSeeker ?? throw new ArgumentNullException(nameof(migrationSeeker));
        _migrationsApplicator = migrationsApplicator ?? throw new ArgumentNullException(nameof(migrationsApplicator));
        _console = console ?? throw new ArgumentNullException(nameof(console));
    }

    public void Run(string[] args)
    {
        _logger.LogTrace($"{nameof(Migrate)} running");

        _logger.LogInformation("Provider: {Provider}", _database.Provider);
        _logger.LogInformation("Version: {ServerVersion}", _database.ServerVersion);
        _logger.LogInformation("Database: {Database}", _database.Connection.Database);

        _console.WriteLine($"{_database.Provider}::{_database.ServerVersion}::{_database.Connection.Database}");

        if (!_database.HasMigrationTable())
        {
            _console.WriteLine(
                "Creating schema history table: " +
                $"\"{_configuration.DefaultSchema}\".\"{_configuration.SchemaTable}\""
            );

            // TODO: How about if the default schema has not been created?
            _database.CreateSchemaHistory();
        }

        var validated = _validator.Validate();

        _console.MarkupLine(
            $"Successfully validated {validated} migrations"
        );

        var discoveredMigrations = _migrationSeeker.Find();

        var appliedMigrations = _database.GetSchemaHistory();

        _console.WriteLine(
            $"Current version of schema \"{_configuration.DefaultSchema}\":" +
            $" {appliedMigrations.Max(x => x.Version) ?? "<< Empty Schema >>"}"
        );

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
            .ThenBy(pendingMigration => pendingMigration.Description);

        // TODO: Conditional based on validateOnMigrate arg
        var appliedCount = _migrationsApplicator.Apply(pendingMigrations, out var elapsedTime);

        if (appliedCount == 0)
        {
            _console.MarkupLine(
                $"[green]Schema \"{_configuration.DefaultSchema}\" is up to date[/]"
            );

            return;
        }

        _console.MarkupLine(
            $"[green]Successfully applied {appliedCount} migration(s) " +
            $"to schema \"{_configuration.DefaultSchema}\" " +
            $@"(execution time {elapsedTime:mm\:ss\.fff}s)[/]"
        );
    }
}