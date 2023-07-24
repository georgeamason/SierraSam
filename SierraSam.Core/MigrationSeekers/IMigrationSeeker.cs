namespace SierraSam.Core.MigrationSeekers;

public interface IMigrationSeeker
{
    IEnumerable<string> Find();
}