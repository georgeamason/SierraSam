using System.Collections;
using System.Data;
using FluentAssertions;
using NSubstitute;
using SierraSam.Core.Enums;
using SierraSam.Core.Exceptions;
using SierraSam.Core.Extensions;
using SierraSam.Core.MigrationApplicators;
using Spectre.Console;

namespace SierraSam.Core.Tests.Unit.MigrationApplicators;

internal sealed class RepeatableMigrationApplicatorTests
{
    private static IEnumerable Constructor_null_args()
    {
        // ReSharper disable ObjectCreationAsStatement
        yield return new TestCaseData(
            new TestDelegate(() => new RepeatableMigrationApplicator(
                    null!,
                    Substitute.For<IConfiguration>(),
                    Substitute.For<IAnsiConsole>(),
                    Substitute.For<TimeProvider>()
                )
            )).SetName("Database is null");

        yield return new TestCaseData(
            new TestDelegate(() => new RepeatableMigrationApplicator(
                    Substitute.For<IDatabase>(),
                    null!,
                    Substitute.For<IAnsiConsole>(),
                    Substitute.For<TimeProvider>()
                )
            )).SetName("configuration is null");

        yield return new TestCaseData(
            new TestDelegate(() => new RepeatableMigrationApplicator(
                    Substitute.For<IDatabase>(),
                    Substitute.For<IConfiguration>(),
                    null!,
                    Substitute.For<TimeProvider>()
                )
            )).SetName("console is null");
        // ReSharper restore ObjectCreationAsStatement
    }

    [TestCaseSource(nameof(Constructor_null_args))]
    public void Constructor_throws_for_null_args(TestDelegate constructor)
    {
        Assert.Throws<ArgumentNullException>(constructor);
    }

    [Test]
    public void Throws_for_wrong_pending_migration_type()
    {
        var sut = new RepeatableMigrationApplicator(
            Substitute.For<IDatabase>(),
            Substitute.For<IConfiguration>(),
            Substitute.For<IAnsiConsole>(),
            Substitute.For<TimeProvider>()
        );

        var pendingMigration = new PendingMigration(
            "someVersion",
            "someDescription",
            MigrationType.Versioned,
            string.Empty,
            string.Empty
        );

        sut.Invoking(applicator => applicator.Apply(pendingMigration, Substitute.For<IDbTransaction>()))
            .Should()
            .Throw<ArgumentException>()
            .WithParameterName(nameof(pendingMigration))
            .WithMessage($"Migration type \"{MigrationType.Versioned}\" is not supported by this applicator. *");
    }

    [Test]
    public void If_checksum_matches_nothing_is_done()
    {
        var database = Substitute.For<IDatabase>();
        var configuration = Substitute.For<IConfiguration>();
        var console = Substitute.For<IAnsiConsole>();

        var sut = new RepeatableMigrationApplicator(database, configuration, console, Substitute.For<TimeProvider>());

        var pendingMigration = new PendingMigration(
            "someVersion",
            "someDescription",
            MigrationType.Repeatable,
            sql: string.Empty,
            string.Empty
        );

        var transaction = Substitute.For<IDbTransaction>();

        database.GetSchemaHistory(transaction: transaction).Returns(new[]
        {
            new AppliedMigration(
                1,
                "someVersion",
                "someDescription",
                "SQL",
                "someFilename",
                checksum: string.Empty.Checksum(),
                "someUser",
                DateTime.UtcNow,
                double.MinValue,
                true)
        });

        var appliedCount = sut.Apply(pendingMigration, transaction);

        database.DidNotReceiveWithAnyArgs().ExecuteMigration(Arg.Any<string>());
        database.DidNotReceiveWithAnyArgs().UpdateSchemaHistory(Arg.Any<AppliedMigration>());
        database.DidNotReceiveWithAnyArgs().InsertSchemaHistory(Arg.Any<AppliedMigration>());

        appliedCount.Should().Be(0);
    }

    [Test]
    public void Altered_checksum_with_filename_match_causes_schema_history_update()
    {
        var database = Substitute.For<IDatabase>();
        var configuration = Substitute.For<IConfiguration>();
        var console = Substitute.For<IAnsiConsole>();

        const string filename = "someFilename";

        var pendingMigration = new PendingMigration(
            "someVersion",
            "someDescription",
            MigrationType.Repeatable,
            sql: "SELECT 1",
            filename
        );

        var sut = new RepeatableMigrationApplicator(database, configuration, console, Substitute.For<TimeProvider>());

        var transaction = Substitute.For<IDbTransaction>();

        database.GetSchemaHistory(transaction: transaction).Returns(new[]
        {
            new AppliedMigration(
                1,
                "someVersion",
                "someDescription",
                "SQL",
                filename,
                checksum: string.Empty.Checksum(),
                "someUser",
                DateTime.UtcNow,
                double.MinValue,
                true)
        });

        var appliedCount = sut.Apply(pendingMigration, transaction);

        database
            .Received()
            .UpdateSchemaHistory(
                Arg.Is<AppliedMigration>(m => m.Checksum == pendingMigration.Checksum),
                transaction
            );

        database
            .Received()
            .ExecuteMigration(pendingMigration.Sql, transaction);

        appliedCount.Should().Be(0);
    }

    [Test]
    public void Fresh_migration_is_applied_to_schema_history()
    {
        var database = Substitute.For<IDatabase>();
        var configuration = Substitute.For<IConfiguration>();
        configuration.InstalledBy.Returns("someUser");
        var console = Substitute.For<IAnsiConsole>();
        var timeProvider = Substitute.For<TimeProvider>();
        timeProvider.GetUtcNow().Returns(new DateTimeOffset(2024, 1, 1, 12, 1, 1, new TimeSpan(0)));

        database.GetSchemaHistory().Returns(Array.Empty<AppliedMigration>());

        var sut = new RepeatableMigrationApplicator(database, configuration, console, timeProvider);

        var pendingMigration = new PendingMigration(
            "someVersion",
            "someDescription",
            MigrationType.Repeatable,
            sql: string.Empty,
            "filename.sql"
        );

        var transaction = Substitute.For<IDbTransaction>();

        database.GetInstalledRank(transaction: transaction).Returns(0);
        database.ExecuteMigration(pendingMigration.Sql, transaction).Returns(TimeSpan.FromSeconds(1));
        database.InsertSchemaHistory(Arg.Any<AppliedMigration>(), transaction).Returns(1);

        var appliedCount = sut.Apply(pendingMigration, transaction);

        database
            .Received()
            .ExecuteMigration(pendingMigration.Sql, transaction);

        database
            .Received()
            .InsertSchemaHistory(
                Arg.Is<AppliedMigration>(
                    migration => migration.InstalledRank == 1 &&
                                 migration.Version == "someVersion" &&
                                 migration.Description == "someDescription" &&
                                 migration.Type == "SQL" &&
                                 migration.Script == "filename.sql" &&
                                 migration.Checksum == string.Empty.Checksum() &&
                                 migration.InstalledBy == "someUser" &&
                                 migration.InstalledOn == new DateTime(2024, 1, 1, 12, 1, 1, DateTimeKind.Utc) &&
                                 migration.ExecutionTime == 1000 &&
                                 migration.Success == true
                ),
                transaction
            );

        appliedCount.Should().Be(1);
    }

    [Test]
    public void Failure_causes_rollback()
    {
        var database = Substitute.For<IDatabase>();
        var configuration = Substitute.For<IConfiguration>();
        configuration.InstalledBy.Returns("someUser");
        var console = Substitute.For<IAnsiConsole>();

        database.GetSchemaHistory().Returns(Array.Empty<AppliedMigration>());

        var sut = new RepeatableMigrationApplicator(database, configuration, console, Substitute.For<TimeProvider>());

        var pendingMigration = new PendingMigration(
            "someVersion",
            "someDescription",
            MigrationType.Repeatable,
            sql: string.Empty,
            "filename.sql"
        );

        var transaction = Substitute.For<IDbTransaction>();

        database
            .WhenForAnyArgs(x => x.InsertSchemaHistory(default!))
            .Do(x => throw new OdbcExecutorException());

        sut.Invoking(applicator => applicator.Apply(pendingMigration, transaction))
            .Should().Throw<MigrationApplicatorException>()
            .WithMessage($"Failed to apply migration \"{pendingMigration.FileName}\"; rolled back the transaction.");

        transaction.Received().Rollback();
    }
}