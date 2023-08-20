using SierraSam.Core.Enums;
using SierraSam.Core.MigrationSeekers;

namespace SierraSam.Core;

public class MigrationMerger : IMigrationMerger
{
    private readonly IMigrationSeeker _migrationSeeker;
    private readonly IDatabase _database;
    private readonly IConfiguration _configuration;

    public MigrationMerger(
        IMigrationSeeker migrationSeeker,
        IDatabase database,
        IConfiguration configuration)
    {
        _migrationSeeker = migrationSeeker ?? throw new ArgumentNullException(nameof(migrationSeeker));
        _database = database ?? throw new ArgumentNullException(nameof(database));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public IReadOnlyCollection<TerseMigration> Merge()
    {
        var discoveredMigrations = _migrationSeeker
            .Find()
            .Select(m =>
            {
                // ReSharper disable once ConvertToLambdaExpression
                return new TerseMigration(
                    m.MigrationType,
                    m.Version,
                    m.Description,
                    "SQL",
                    m.Checksum,
                    null,
                    MigrationState.Pending);
            });

        return _database
            .GetSchemaHistory(_configuration.DefaultSchema, _configuration.SchemaTable)
            .Select(m =>
            {
                var isDiscovered = discoveredMigrations
                    .Select(x => x.Checksum)
                    .Contains(m.Checksum);

                return new TerseMigration(
                    m.MigrationType,
                    m.Version,
                    m.Description,
                    m.Type,
                    m.Checksum,
                    m.InstalledOn,
                    isDiscovered ? MigrationState.Applied : MigrationState.Missing);
            })
            .UnionBy(discoveredMigrations, migration => migration.Checksum)
            .OrderBy(m => m.InstalledOn ?? DateTime.MaxValue)
            .ThenBy(m => m.Version)
            .ToArray();
    }
}