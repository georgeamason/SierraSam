namespace SierraSam.Core.MigrationValidators;

public interface IMigrationValidator
{
    /// <summary>
    /// Validates migrations
    /// </summary>
    /// <returns>The number of migrations validated</returns>
    int Validate();
}