using SierraSam.Core.MigrationSeekers;
using SierraSam.Core.MigrationValidators;

namespace SierraSam.Core.Factories;

public static class MigrationValidatorFactory
{
    public static IMigrationValidator Create(
        IMigrationSeeker migrationSeeker,
        IDatabase database,
        IIgnoredMigrationsFactory ignoredMigrationsFactory)
    {
        return new LocalMigrationValidator(
            migrationSeeker,
            database,
            ignoredMigrationsFactory,
            new RemoteMigrationValidator(
                migrationSeeker,
                database,
                ignoredMigrationsFactory,
                new DistinctVersionMigrationValidator(
                    migrationSeeker,
                    new DistinctChecksumMigrationValidator(
                        migrationSeeker)
                    )
                )
            );
    }
}