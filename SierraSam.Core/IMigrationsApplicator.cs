using SierraSam.Core.Exceptions;

namespace SierraSam.Core;

public interface IMigrationsApplicator
{
    /// <summary>
    /// Apply migrations to the database
    /// </summary>
    /// <exception cref="MigrationApplicatorException"></exception>
    int Apply(IReadOnlyCollection<PendingMigration> pendingMigrations);
}