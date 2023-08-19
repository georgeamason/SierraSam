using System.Collections.Immutable;
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
        var discoveredMigrations = _migrationSeeker.Find();

        var distinctMigrations = discoveredMigrations
            .DistinctBy(m => m.Checksum)
            .ToArray();

        if (distinctMigrations.Length != discoveredMigrations.Count)
        {
            throw new Exception
                ($"Discovered multiple migrations with equal contents");
        }

        return discoveredMigrations.Count;
    }
}