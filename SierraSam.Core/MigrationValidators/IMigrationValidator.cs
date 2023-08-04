namespace SierraSam.Core.MigrationValidators;

public interface IMigrationValidator
{
    TimeSpan Validate
        (IReadOnlyCollection<AppliedMigration> appliedMigrations,
         IReadOnlyCollection<PendingMigration> discoveredMigrations);
}