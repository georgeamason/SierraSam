using System.Collections;
using System.Data;
using FluentAssertions;
using NSubstitute;
using SierraSam.Core.Enums;
using SierraSam.Core.Exceptions;
using SierraSam.Core.Extensions;
using SierraSam.Core.MigrationApplicators;
using Spectre.Console;
using Spectre.Console.Testing;

namespace SierraSam.Core.Tests.Unit.MigrationApplicators;

internal sealed class VersionedMigrationApplicatorTests
{
    private static IEnumerable Constructor_null_args()
    {
        // ReSharper disable ObjectCreationAsStatement
        yield return new TestCaseData(
            new TestDelegate(() => new VersionedMigrationApplicator(
                    null!,
                Substitute.For<IConfiguration>(),
                Substitute.For<IAnsiConsole>(),
                Substitute.For<TimeProvider>()
            )
        )).SetName("Database is null");

        yield return new TestCaseData(
            new TestDelegate(() => new VersionedMigrationApplicator(
                    Substitute.For<IDatabase>(),
                    null!,
                    Substitute.For<IAnsiConsole>(),
                    Substitute.For<TimeProvider>()
                )
            )).SetName("configuration is null");

        yield return new TestCaseData(
            new TestDelegate(() => new VersionedMigrationApplicator(
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
    public void Throws_when_pending_migration_type_is_not_versioned()
    {
        var database = Substitute.For<IDatabase>();
        var configuration = Substitute.For<IConfiguration>();
        var console = new TestConsole();

        var sut = new VersionedMigrationApplicator(database, configuration, console, Substitute.For<TimeProvider>());

        var pendingMigration = new PendingMigration(
            "1",
            "someDescription",
            MigrationType.Repeatable,
            string.Empty,
            string.Empty
        );

        sut.Invoking(applicator => applicator.Apply(pendingMigration, Substitute.For<IDbTransaction>()))
            .Should()
            .Throw<ArgumentException>()
            .WithParameterName(nameof(pendingMigration))
            .WithMessage($"Migration type \"{MigrationType.Repeatable}\" is not supported by this applicator. *");
    }

    [Test]
    public void Feedback_is_written_to_console()
    {
        var database = Substitute.For<IDatabase>();
        var configuration = Substitute.For<IConfiguration>();
        configuration.InstalledBy.Returns("someUser");
        var console = new TestConsole();

        var sut = new VersionedMigrationApplicator(database, configuration, console, Substitute.For<TimeProvider>());

        var pendingMigration = new PendingMigration(
            "1",
            "someDescription",
            MigrationType.Versioned,
            string.Empty,
            "filename.sql"
        );

        sut.Apply(pendingMigration, Substitute.For<IDbTransaction>());

        console
            .Output
            .TrimEnd()
            .Should()
            .Be($"Migrating schema \"{configuration.DefaultSchema}\" " +
                $"to version {pendingMigration.Version} - {pendingMigration.Description}");
    }

    [Test]
    public void Apply_executes_migration_and_inserts_into_schema_history()
    {
        var database = Substitute.For<IDatabase>();
        var configuration = Substitute.For<IConfiguration>();
        configuration.InstalledBy.Returns("someUser");
        var console = Substitute.For<IAnsiConsole>();
        var timeProvider = Substitute.For<TimeProvider>();
        timeProvider.GetUtcNow().Returns(new DateTimeOffset(2024, 1, 1, 12, 1, 1, new TimeSpan(0)));

        var sut = new VersionedMigrationApplicator(database, configuration, console,timeProvider);

        var pendingMigration = new PendingMigration(
            "1",
            "someDescription",
            MigrationType.Versioned,
            string.Empty,
            "filename.sql"
        );

        var transaction = Substitute.For<IDbTransaction>();

        database.GetInstalledRank(transaction: transaction).Returns(0);
        database.ExecuteMigration(pendingMigration.Sql, transaction).Returns(TimeSpan.FromSeconds(2));

        sut.Apply(pendingMigration, transaction);

        database
            .Received(1)
            .ExecuteMigration(pendingMigration.Sql, transaction);

        database
            .Received(1)
            .InsertSchemaHistory(
                Arg.Is<AppliedMigration>(appliedMigration =>
                    appliedMigration.InstalledRank == 1 &&
                    appliedMigration.Version == "1" &&
                    appliedMigration.Description == "someDescription" &&
                    appliedMigration.Type == "SQL" &&
                    appliedMigration.Script == "filename.sql" &&
                    appliedMigration.Checksum == string.Empty.Checksum() &&
                    appliedMigration.InstalledBy == configuration.InstalledBy &&
                    appliedMigration.InstalledOn == new DateTime(2024, 1, 1, 12, 1, 1, DateTimeKind.Utc) &&
                    appliedMigration.ExecutionTime == 2000 &&
                    appliedMigration.Success == true
                ),
                transaction
            );
    }

    [Test]
    public void Apply_rolls_back_transaction_on_error()
    {
        var database = Substitute.For<IDatabase>();
        var configuration = Substitute.For<IConfiguration>();
        var console = Substitute.For<IAnsiConsole>();

        var sut = new VersionedMigrationApplicator(database, configuration, console, Substitute.For<TimeProvider>());

        var pendingMigration = new PendingMigration(
            "1",
            "someDescription",
            MigrationType.Versioned,
            string.Empty,
            string.Empty
        );

        var transaction = Substitute.For<IDbTransaction>();

        database
            .When(db => db.ExecuteMigration(pendingMigration.Sql, transaction))
            .Do(_ => throw new OdbcExecutorException("some message"));

        sut.Invoking(applicator => applicator.Apply(pendingMigration, transaction))
            .Should()
            .Throw<MigrationApplicatorException>()
            .WithMessage("Failed to apply migration \"\"; rolled back the transaction.")
            .WithInnerException<Exception>()
            .WithMessage("some message");

        transaction.Received(1).Rollback();
    }
}