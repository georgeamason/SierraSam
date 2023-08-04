using System.IO.Abstractions;
using SierraSam.Core.MigrationValidators;

namespace SierraSam.Core.Factories;

public static class MigrationValidatorFactory
{
    public static IMigrationValidator Create(Configuration configuration, IFileSystem fileSystem)
    {
        ArgumentNullException.ThrowIfNull(fileSystem, nameof(fileSystem));
        ArgumentNullException.ThrowIfNull(configuration, nameof(configuration));

        return new LocalMigrationValidator
            (fileSystem, new RemoteMigrationValidator
                (fileSystem, configuration,
                    new DistinctVersionMigrationValidator()));
    }
}