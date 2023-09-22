namespace SierraSam.Core.Extensions;

public static class AppliedMigrationExtensions
{
    public static AppliedMigration WithChecksum
        (this AppliedMigration appliedMigration, string checksum)
        => new (appliedMigration.InstalledRank,
                appliedMigration.Version,
                appliedMigration.Description,
                appliedMigration.Type,
                appliedMigration.Script,
                checksum,
                appliedMigration.InstalledBy,
                appliedMigration.InstalledOn,
                appliedMigration.ExecutionTime,
                appliedMigration.Success);

    public static AppliedMigration WithInstalledOn
        (this AppliedMigration appliedMigration, DateTime installedOn)
        => new (appliedMigration.InstalledRank,
            appliedMigration.Version,
            appliedMigration.Description,
            appliedMigration.Type,
            appliedMigration.Script,
            appliedMigration.Checksum,
            appliedMigration.InstalledBy,
            installedOn,
            appliedMigration.ExecutionTime,
            appliedMigration.Success);
}