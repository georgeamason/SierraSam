using System.Text.RegularExpressions;
using SierraSam.Core.Exceptions;

namespace SierraSam.Core.MigrationSeekers;

public interface IMigrationSeeker
{
    /// <summary>
    /// Find pending migrations from the configured locations.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="MigrationSeekerException">
    /// Can be thrown based on the following exceptions:
    /// <see cref="UnauthorizedAccessException"/>
    /// <see cref="DirectoryNotFoundException"/>
    /// <see cref="PathTooLongException"/>
    /// <see cref="RegexMatchTimeoutException"/>
    /// </exception>
    IReadOnlyCollection<PendingMigration> GetPendingMigrations();
}