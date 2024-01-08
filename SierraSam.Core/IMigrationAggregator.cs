namespace SierraSam.Core;

public interface IMigrationAggregator
{
    /// <summary>
    /// Get discovered and applied migrations.
    /// </summary>
    /// <returns>A collection of terse migrations</returns>
    IReadOnlyCollection<TerseMigration> GetAllMigrations();
}