namespace SierraSam.Core;

public interface IMigrationMerger
{
    /// <summary>
    /// Merge discovered migrations with applied migrations.
    /// </summary>
    /// <returns>A collection of terse migrations</returns>
    IReadOnlyCollection<TerseMigration> Merge();
}