using FluentAssertions;
using NSubstitute;
using SierraSam.Core.Enums;
using SierraSam.Core.MigrationSeekers;

namespace SierraSam.Core.Tests.Unit;

[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
internal sealed class MigrationMergerTests
{
    private static readonly IMigrationSeeker MigrationSeeker = Substitute.For<IMigrationSeeker>();
    private static readonly IDatabase Database = Substitute.For<IDatabase>();
    private static readonly IConfiguration Configuration = Substitute.For<IConfiguration>();

    private readonly IMigrationMerger _sut = new MigrationMerger(
        MigrationSeeker,
        Database,
        Configuration
    );

    [Test]
    public void Merge_returns_expected_applied_migrations()
    {
        MigrationSeeker
            .GetPendingMigrations()
            .Returns(new[]
            {
                CreatePendingMigration(MigrationType.Versioned)
            });

        var installedOn = DateTime.UtcNow;

        Database
            .GetAppliedMigrations(Arg.Any<string>(), Arg.Any<string>())
            .Returns(new[]
            {
                CreateAppliedMigration(installedOn, "d41d8cd98f00b204e9800998ecf8427e")
            });

        _sut
            .Merge()
            .Should()
            .Equal(new TerseMigration(
                    MigrationType.Versioned,
                    "1",
                    "someDescription",
                    "SQL",
                    "d41d8cd98f00b204e9800998ecf8427e",
                    installedOn,
                    MigrationState.Applied
                )
            );
    }

    [Test]
    public void Merge_returns_expected_missing_migrations()
    {
        MigrationSeeker
            .GetPendingMigrations()
            .Returns(Array.Empty<PendingMigration>());

        var installedOn = DateTime.UtcNow;

        Database
            .GetAppliedMigrations(Arg.Any<string>(), Arg.Any<string>())
            .Returns(new[]
            {
                CreateAppliedMigration(installedOn, "abcd")
            });

        _sut
            .Merge()
            .Should()
            .Equal(new TerseMigration(
                MigrationType.Versioned,
                "1",
                "someDescription",
                "SQL",
                "abcd",
                installedOn,
                MigrationState.Missing));
    }

    [Test]
    public void Merge_returns_expected_pending_migrations()
    {
        MigrationSeeker
            .GetPendingMigrations()
            .Returns(new []
            {
                CreatePendingMigration(MigrationType.Versioned)
            });

        Database
            .GetAppliedMigrations(Arg.Any<string>(), Arg.Any<string>())
            .Returns(Array.Empty<AppliedMigration>());

        _sut
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
        new(
            1,
            "1",
            "someDescription",
            "SQL",
            "filename.sql",
            checksum,
            "someUser",
            installedOn,
            double.MinValue,
            true
        );

    private static PendingMigration CreatePendingMigration(MigrationType migrationType) =>
        new(
            "1",
            "description",
            migrationType,
            string.Empty,
            string.Empty
        );
}

