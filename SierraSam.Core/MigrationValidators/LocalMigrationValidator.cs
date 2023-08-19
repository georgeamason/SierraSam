using SierraSam.Core.Enums;
using SierraSam.Core.Factories;
using SierraSam.Core.MigrationSeekers;

namespace SierraSam.Core.MigrationValidators;

/// <summary>
/// Validation fails if local migrations haven't been applied
/// to the database.
/// </summary>
internal sealed class LocalMigrationValidator : IMigrationValidator
{
    private readonly IMigrationValidator _validator;
    private readonly IMigrationSeeker _migrationSeeker;
    private readonly IDatabase _database;
    private readonly IIgnoredMigrationsFactory _ignoredMigrationsFactory;

    public LocalMigrationValidator(
        IMigrationSeeker migrationSeeker,
        IDatabase database,
        IIgnoredMigrationsFactory ignoredMigrationsFactory,
        IMigrationValidator validator)
    {
        _validator = validator
            ?? throw new ArgumentNullException(nameof(validator));

        _migrationSeeker = migrationSeeker
            ?? throw new ArgumentNullException(nameof(migrationSeeker));

        _database = database
            ?? throw new ArgumentNullException(nameof(database));

        _ignoredMigrationsFactory = ignoredMigrationsFactory
            ?? throw new ArgumentNullException(nameof(ignoredMigrationsFactory));
    }

    public int Validate()
    {
        var validated = _validator.Validate();

        // TODO: This could probably be injected from the MigrationValidatorFactory class tbh
        var ignoredMigrations = _ignoredMigrationsFactory.Create();

        var shortCircuit = ignoredMigrations.Any(i => i is
        {
            Type: MigrationType.Any,
            State: MigrationState.Any or MigrationState.Pending
        });

        if (shortCircuit) return validated;

        var ignoredMigrationTypes = ignoredMigrations
            .Where(i => i.State is MigrationState.Any or MigrationState.Pending)
            .Select(p => p.Type)
            .ToArray();

        var filteredDiscoveredMigrations = _migrationSeeker
            .Find()
            .Where(m => ignoredMigrationTypes switch
            {
                [MigrationType.Repeatable] => m.MigrationType is not MigrationType.Repeatable,
                [MigrationType.Versioned] => m.MigrationType is not MigrationType.Versioned,
                [MigrationType.Repeatable, MigrationType.Versioned] => false,
                [MigrationType.Versioned, MigrationType.Repeatable] => false,
                _ => true
            });

        foreach (var discoveredMigration in filteredDiscoveredMigrations)
        {
            var appliedMigration = _database
                .GetSchemaHistory()
                .SingleOrDefault(m => m.Version == discoveredMigration.Version &&
                                      m.Script == discoveredMigration.FileName &&
                                      m.Type == "SQL" &&
                                      m.Checksum == discoveredMigration.Checksum);

            if (appliedMigration is null)
            {
                // TODO: Make custom exception
                throw new Exception(
                    $"Unable to find remote migration {discoveredMigration.FileName}");
            }
        }

        return validated;
    }
}