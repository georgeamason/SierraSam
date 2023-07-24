using System.IO.Abstractions;
using SierraSam.Core.MigrationSeekers;

namespace SierraSam.Core.Factories;

public static class MigrationSeekerFactory
{
    public static IMigrationSeeker Create(Configuration configuration, IFileSystem fileSystem)
    {
        ArgumentNullException.ThrowIfNull(configuration, nameof(configuration));
        ArgumentNullException.ThrowIfNull(fileSystem, nameof(fileSystem));

        return new AwsStorageMigrationSeeker
            (new FileSystemMigrationSeeker
                (configuration, fileSystem));
    }
}