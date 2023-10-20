using System.Collections;
using System.Data;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NSubstitute.ReceivedExtensions;
using SierraSam.Core.Enums;
using SierraSam.Core.MigrationApplicators;

namespace SierraSam.Core.Tests.Unit;

internal sealed class MigrationsApplicatorTests
{
    private static IEnumerable Constructors_with_null_args()
    {
        // ReSharper disable ObjectCreationAsStatement
        yield return new TestCaseData
            (new TestDelegate
            (() => new MigrationsApplicator
            (null!,
                Substitute.For<IMigrationApplicatorResolver>())))
            .SetName("null database");

        yield return new TestCaseData
            (new TestDelegate
            (() => new MigrationsApplicator
            (Substitute.For<IDatabase>(),
                null!)))
            .SetName("null resolver");
        // ReSharper enable ObjectCreationAsStatement
    }

    [TestCaseSource(nameof(Constructors_with_null_args))]
    public void Constructor_throws_for_null_arguments(TestDelegate constructor)
    {
        Assert.Throws<ArgumentNullException>(constructor);
    }

    [Test]
    public void Correct_migration_applicator_is_called()
    {
        var database = Substitute.For<IDatabase>();
        var dbConnection = Substitute.For<IDbConnection>();
        var dbTransaction = Substitute.For<IDbTransaction>();

        database
            .Connection
            .Returns(dbConnection);

        dbConnection
            .BeginTransaction()
            .Returns(dbTransaction);

        var migrationApplicatorResolver = Substitute.For<IMigrationApplicatorResolver>();

        var sut = new MigrationsApplicator(database, migrationApplicatorResolver);

        var pendingMigrations = new[]
        {
            new PendingMigration(
                "someVersion",
                "someDescription",
                MigrationType.Versioned,
                string.Empty,
                string.Empty
            ),
            new PendingMigration(
                "anotherVersion",
                "anotherDescription",
                MigrationType.Repeatable,
                string.Empty,
                string.Empty
            )
        };

        var versionMigrationApplicator = Substitute.For<IMigrationApplicator>();

        versionMigrationApplicator
            .Apply(pendingMigrations[0], dbTransaction)
            .Returns(1);

        migrationApplicatorResolver
            .Resolve(typeof(VersionedMigrationApplicator))
            .Returns(versionMigrationApplicator);

        var repeatableMigrationApplicator = Substitute.For<IMigrationApplicator>();

        repeatableMigrationApplicator
            .Apply(pendingMigrations[1], dbTransaction)
            .Returns(1);

        migrationApplicatorResolver
            .Resolve(typeof(RepeatableMigrationApplicator))
            .Returns(repeatableMigrationApplicator);

        var appliedCount = sut.Apply(pendingMigrations);

        versionMigrationApplicator
            .Received(1)
            .Apply(pendingMigrations[0], dbTransaction);

        repeatableMigrationApplicator
            .Received(1)
            .Apply(pendingMigrations[1], dbTransaction);

        dbTransaction.Received().Commit();

        appliedCount.Should().Be(2);
    }

    [Test]
    public void Argument_exception_thrown_for_unknown_migration_type()
    {
        var database = Substitute.For<IDatabase>();
        var dbConnection = Substitute.For<IDbConnection>();
        var dbTransaction = Substitute.For<IDbTransaction>();

        database
            .Connection
            .Returns(dbConnection);

        dbConnection
            .BeginTransaction()
            .Returns(dbTransaction);

        var migrationApplicatorResolver = Substitute.For<IMigrationApplicatorResolver>();

        var sut = new MigrationsApplicator(database, migrationApplicatorResolver);

        var pendingMigrations = new[]
        {
            new PendingMigration(
                "someVersion",
                "someDescription",
                MigrationType.None,
                string.Empty,
                string.Empty
            )
        };

        var versionMigrationApplicator = Substitute.For<IMigrationApplicator>();

        migrationApplicatorResolver
            .Resolve(typeof(VersionedMigrationApplicator))
            .Returns(versionMigrationApplicator);

        sut.Invoking(applicator => applicator.Apply(pendingMigrations))
            .Should()
            .Throw<ArgumentOutOfRangeException>();
    }
}