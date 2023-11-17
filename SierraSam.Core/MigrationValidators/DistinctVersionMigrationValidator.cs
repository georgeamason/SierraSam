using SierraSam.Core.Exceptions;
using SierraSam.Core.MigrationSeekers;
using static SierraSam.Core.Enums.MigrationType;

namespace SierraSam.Core.MigrationValidators;

/// <summary>
/// Checks that there are no discovered migrations that have the same version
/// </summary>
internal sealed class DistinctVersionMigrationValidator : IMigrationValidator
{
    private readonly IMigrationSeeker _migrationSeeker;
    private readonly IMigrationValidator _validator;

    public DistinctVersionMigrationValidator(
        IMigrationSeeker migrationSeeker,
        IMigrationValidator validator
    )
    {
        _migrationSeeker = migrationSeeker ?? throw new ArgumentNullException(nameof(migrationSeeker));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
    }

    public int Validate()
    {
        var validated = _validator.Validate();

        var versionedMigrations = _migrationSeeker.Find()
            .Where(m => m.MigrationType is Versioned)
            .ToArray();

        var distinctMigrations = versionedMigrations
            .DistinctBy(m => m.Version)
            .ToArray();

        if (distinctMigrations.Length != versionedMigrations.Length)
        {
            throw new MigrationValidatorException(
                $"Discovered multiple migrations with version {distinctMigrations[0].Version}"
            );
        }

        return validated;
    }
}