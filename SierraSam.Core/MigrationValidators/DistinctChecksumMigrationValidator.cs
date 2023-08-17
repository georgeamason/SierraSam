namespace SierraSam.Core.MigrationValidators;

internal sealed class DistinctChecksumMigrationValidator : IMigrationValidator
{
    // TODO: Check that the checksums are all different
    public TimeSpan Validate
        (IReadOnlyCollection<AppliedMigration> appliedMigrations,
         IReadOnlyCollection<PendingMigration> discoveredMigrations)
    {
        throw new NotImplementedException();
    }
}