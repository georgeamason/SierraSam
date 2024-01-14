using SierraSam.Core.Exceptions;
using SierraSam.Core.MigrationSeekers;

namespace SierraSam.Core.MigrationValidators;

internal sealed class DistinctChecksumMigrationValidator : IMigrationValidator
{
    private readonly IMigrationSeeker _migrationSeeker;

    public DistinctChecksumMigrationValidator(IMigrationSeeker migrationSeeker)
    {
        _migrationSeeker = migrationSeeker
            ?? throw new ArgumentNullException(nameof(migrationSeeker));
    }

    public int Validate()
    {
        var discoveredMigrations = _migrationSeeker.GetPendingMigrations();

        var distinctMigrations = discoveredMigrations.DistinctBy(m => m.Checksum);

        if (distinctMigrations.Count() != discoveredMigrations.Count)
        {
            throw new MigrationValidatorException(
                "Discovered multiple migrations with equal contents"
            );
        }

        return discoveredMigrations.Count;
    }
}