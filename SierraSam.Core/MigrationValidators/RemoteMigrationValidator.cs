using System.Collections.Immutable;
using SierraSam.Core.Enums;
using SierraSam.Core.Exceptions;
using SierraSam.Core.Factories;
using SierraSam.Core.MigrationSeekers;
using static SierraSam.Core.Enums.MigrationState;
using static SierraSam.Core.Enums.MigrationType;

namespace SierraSam.Core.MigrationValidators;

/// <summary>
/// Validation fails if applied migrations haven't been discovered
/// locally by the migration seeker.
/// </summary>
internal sealed class RemoteMigrationValidator : IMigrationValidator
{
    private readonly IMigrationValidator _validator;
    private readonly IMigrationSeeker _migrationSeeker;
    private readonly IDatabase _database;
    private readonly IIgnoredMigrationsFactory _ignoredMigrationsFactory;

    public RemoteMigrationValidator(
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

        var ignoredMigrations = _ignoredMigrationsFactory.Create();

        var shortCircuit = ignoredMigrations.Any(i => i is
        {
            Type: MigrationType.Any,
            State: MigrationState.Any or Missing
        });

        if (shortCircuit) return validated;

        var migrationTypesToIgnore = ignoredMigrations
            .Where(i => i.State is Missing or MigrationState.Any)
            .Select(i => i.Type)
            .ToImmutableArray();

        var filteredAppliedMigrations = _database
            .GetAppliedMigrations()
            .Where(m => migrationTypesToIgnore switch
            {
                [Repeatable] => m.MigrationType is not Repeatable,
                [Versioned] => m.MigrationType is not Versioned,
                [Repeatable, Versioned] => false,
                [Versioned, Repeatable] => false,
                _ => true
            });

        foreach (var appliedMigration in filteredAppliedMigrations)
        {
            var discoveredMigration = _migrationSeeker
                .Find()
                .SingleOrDefault(m => m.Version == appliedMigration.Version &&
                                      m.FileName == appliedMigration.Script &&
                                      "SQL" == appliedMigration.Type &&
                                      m.Checksum == appliedMigration.Checksum);

            if (discoveredMigration is null)
            {
                throw new MigrationValidatorException(
                    $"Unable to find local migration {appliedMigration.Script}"
                );
            }
        }

        return validated;
    }
}