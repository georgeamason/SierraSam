using System.Diagnostics;
using SierraSam.Core.Enums;
using SierraSam.Core.MigrationSeekers;

namespace SierraSam.Core.MigrationValidators;

/// <summary>
/// Checks that there are no discovered migrations that have the same version
/// </summary>
internal sealed class DistinctVersionMigrationValidator : IMigrationValidator
{
    private readonly IMigrationSeeker _migrationSeeker;
    private readonly IMigrationValidator _validator;

    public DistinctVersionMigrationValidator(IMigrationSeeker migrationSeeker, IMigrationValidator validator)
    {
        _migrationSeeker = migrationSeeker ?? throw new ArgumentNullException(nameof(migrationSeeker));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
    }

    public int Validate()
    {
        var validated = _validator.Validate();

        var discoveredVersionedMigrations = _migrationSeeker
            .Find()
            .Where(m => m.MigrationType is MigrationType.Versioned)
            .ToArray();

        var distinctMigrations = discoveredVersionedMigrations
            .DistinctBy(m => m.Version)
            .ToArray();

        if (distinctMigrations.Length != discoveredVersionedMigrations.Length)
        {
            throw new Exception
                ($"Discovered multiple migrations with version {distinctMigrations[0].Version}");
        }

        return validated;
    }
}