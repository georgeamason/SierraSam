namespace SierraSam.Core.MigrationSeekers;

internal sealed class AwsStorageMigrationSeeker : IMigrationSeeker
{
    private readonly IMigrationSeeker _migrationSeeker;

    public AwsStorageMigrationSeeker(IMigrationSeeker migrationSeeker)
    {
        _migrationSeeker = migrationSeeker ?? throw new ArgumentNullException(nameof(migrationSeeker));
    }

    public IReadOnlyCollection<PendingMigration> GetPendingMigrations()
    {
        var migrations = _migrationSeeker.GetPendingMigrations();

        // TODO: Search s3 buckets for migrations

        return migrations;
    }
}