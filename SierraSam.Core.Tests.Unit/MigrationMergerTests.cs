using FluentAssertions;
using NSubstitute;
using SierraSam.Core.Enums;
using SierraSam.Core.MigrationSeekers;

namespace SierraSam.Core.Tests.Unit;

[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
internal sealed class MigrationMergerTests
{
    private readonly IMigrationSeeker _migrationSeeker;
    private readonly IDatabase _database;
    private readonly IMigrationMerger _migrationMerger;

    public MigrationMergerTests()
    {
        _migrationSeeker = Substitute.For<IMigrationSeeker>();
        _database = Substitute.For<IDatabase>();
        var configuration = Substitute.For<IConfiguration>();

        _migrationMerger = new MigrationMerger(
            _migrationSeeker,
            _database,
            configuration);
    }

    [Test]
    public void Merge_returns_expected_applied_migrations()
    {
        _migrationSeeker
            .Find()
            .Returns(new[]
            {
                CreatePendingMigration(MigrationType.Versioned)
            });

        var installedOn = DateTime.UtcNow;

        _database
            .GetSchemaHistory(Arg.Any<string>(), Arg.Any<string>())
            .Returns(new[]
            {
                CreateAppliedMigration(installedOn, "d41d8cd98f00b204e9800998ecf8427e")
            });

        _migrationMerger
            .Merge()
            .Should()
            .Equal(new TerseMigration(
                MigrationType.Versioned,
                "1",
                string.Empty,
                "SQL",
                "d41d8cd98f00b204e9800998ecf8427e",
                installedOn,
                MigrationState.Applied));
    }

    [Test]
    public void Merge_returns_expected_missing_migrations()
    {
        _migrationSeeker
            .Find()
            .Returns(Array.Empty<PendingMigration>());

        var installedOn = DateTime.UtcNow;

        _database
            .GetSchemaHistory(Arg.Any<string>(), Arg.Any<string>())
            .Returns(new[]
            {
                CreateAppliedMigration(installedOn, "abcd")
            });

        _migrationMerger
            .Merge()
            .Should()
            .Equal(new TerseMigration(
                MigrationType.Versioned,
                "1",
                string.Empty,
                "SQL",
                "abcd",
                installedOn,
                MigrationState.Missing));
    }

    [Test]
    public void Merge_returns_expected_pending_migrations()
    {
        _migrationSeeker
            .Find()
            .Returns(new []
            {
                CreatePendingMigration(MigrationType.Versioned)
            });

        _database
            .GetSchemaHistory(Arg.Any<string>(), Arg.Any<string>())
            .Returns(Array.Empty<AppliedMigration>());

        _migrationMerger
            .Merge()
            .Should()
            .Equal(new TerseMigration(
                MigrationType.Versioned,
                "1",
                "description",
                "SQL",
                "d41d8cd98f00b204e9800998ecf8427e",
                null,
                MigrationState.Pending));
    }

    private static AppliedMigration CreateAppliedMigration(DateTime installedOn, string checksum) =>
        new(1,
            "1",
            string.Empty,
            "SQL",
            string.Empty,
            checksum,
            string.Empty,
            installedOn,
            double.MinValue,
            true);

    private static PendingMigration CreatePendingMigration(MigrationType migrationType) =>
        new("1",
            "description",
            migrationType,
            string.Empty,
            string.Empty);
}

