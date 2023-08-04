namespace SierraSam.Core.MigrationSeekers;

internal sealed class AwsStorageMigrationSeeker : IMigrationSeeker
{
    private readonly IMigrationSeeker _migrationSeeker;

    public AwsStorageMigrationSeeker(IMigrationSeeker migrationSeeker)
    {
        _migrationSeeker = migrationSeeker ?? throw new ArgumentNullException(nameof(migrationSeeker));
    }

    public IReadOnlyCollection<PendingMigration> Find()
    {
        var migrations = _migrationSeeker.Find();

        // TODO: Search s3 buckets for migrations

        return migrations;
    }
}