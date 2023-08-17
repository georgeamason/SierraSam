using System.Diagnostics;
using SierraSam.Core.Enums;

namespace SierraSam.Core.MigrationValidators;

/// <summary>
/// Checks that there are no discovered migrations that have the same version
/// </summary>
internal sealed class DistinctVersionMigrationValidator : IMigrationValidator
{
    public TimeSpan Validate
        (IReadOnlyCollection<AppliedMigration> appliedMigrations,
         IReadOnlyCollection<PendingMigration> discoveredMigrations)
    {
        if (appliedMigrations == null) throw new ArgumentNullException(nameof(appliedMigrations));
        if (discoveredMigrations == null) throw new ArgumentNullException(nameof(discoveredMigrations));

        var stopwatch = new Stopwatch();
        stopwatch.Start();

        var discoveredVersionedMigrations = discoveredMigrations
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

        stopwatch.Stop();

        return stopwatch.Elapsed;
    }
}