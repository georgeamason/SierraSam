using SierraSam.Core.Exceptions;

namespace SierraSam.Core;

public interface IMigrationApplicator
{
    /// <summary>
    /// Apply migrations to the database
    /// </summary>
    /// <exception cref="MigrationApplicatorException"></exception>
    (int appliedMigrations, TimeSpan executionTime) Apply
        (IReadOnlyCollection<PendingMigration> pendingMigrations,
         IReadOnlyCollection<AppliedMigration> appliedMigrations);
}