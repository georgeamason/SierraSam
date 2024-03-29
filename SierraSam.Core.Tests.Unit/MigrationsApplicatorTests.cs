﻿using System.Collections;
using System.Data;
using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using SierraSam.Core.Enums;
using SierraSam.Core.MigrationApplicators;

namespace SierraSam.Core.Tests.Unit;

[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
internal sealed class MigrationsApplicatorTests
{
    private readonly IDatabase _database = Substitute.For<IDatabase>();
    private readonly IMigrationApplicatorResolver _resolver = Substitute.For<IMigrationApplicatorResolver>();
    private readonly FakeTimeProvider _timeProvider = new();
    private readonly MigrationsApplicator _sut;

    public MigrationsApplicatorTests() => _sut = new(_database, _resolver, _timeProvider);

    private static IEnumerable Constructors_with_null_args()
    {
        // ReSharper disable ObjectCreationAsStatement
        yield return new TestCaseData(
                new TestDelegate(
                    () => new MigrationsApplicator(
                        null!,
                        Substitute.For<IMigrationApplicatorResolver>(),
                        Substitute.For<TimeProvider>()
                    )
                )
            )
            .SetName("null database");

        yield return new TestCaseData(
                new TestDelegate(
                    () => new MigrationsApplicator(
                        Substitute.For<IDatabase>(),
                        null!,
                        Substitute.For<TimeProvider>()
                    )
                )
            )
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
        var dbConnection = Substitute.For<IDbConnection>();
        var dbTransaction = Substitute.For<IDbTransaction>();

        _database
            .Connection
            .Returns(dbConnection);

        dbConnection
            .BeginTransaction()
            .Returns(dbTransaction);

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

        _resolver
            .Resolve(typeof(VersionedMigrationApplicator))
            .Returns(versionMigrationApplicator);

        var repeatableMigrationApplicator = Substitute.For<IMigrationApplicator>();

        repeatableMigrationApplicator
            .Apply(pendingMigrations[1], dbTransaction)
            .Returns(1);

        _resolver
            .Resolve(typeof(RepeatableMigrationApplicator))
            .Returns(repeatableMigrationApplicator);

        _sut.Apply(pendingMigrations, out _);

        versionMigrationApplicator
            .Received(1)
            .Apply(pendingMigrations[0], dbTransaction);

        repeatableMigrationApplicator
            .Received(1)
            .Apply(pendingMigrations[1], dbTransaction);

        dbTransaction.Received().Commit();
    }

    [Test]
    public void Argument_exception_thrown_for_unknown_migration_type()
    {
        var dbConnection = Substitute.For<IDbConnection>();
        var dbTransaction = Substitute.For<IDbTransaction>();

        _database
            .Connection
            .Returns(dbConnection);

        dbConnection
            .BeginTransaction()
            .Returns(dbTransaction);

        var pendingMigrations = new[]
        {
            new PendingMigration(
                "someVersion",
                "someDescription",
                MigrationType.Any,
                string.Empty,
                string.Empty
            )
        };

        var versionMigrationApplicator = Substitute.For<IMigrationApplicator>();

        _resolver
            .Resolve(typeof(VersionedMigrationApplicator))
            .Returns(versionMigrationApplicator);

        _sut.Invoking(applicator => applicator.Apply(pendingMigrations, out _))
            .Should()
            .Throw<ArgumentOutOfRangeException>();
    }
}