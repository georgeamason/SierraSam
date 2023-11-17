using System.Collections;
using FluentAssertions;
using NSubstitute;
using SierraSam.Core.Enums;
using SierraSam.Core.Factories;
using SierraSam.Core.MigrationSeekers;
using SierraSam.Core.MigrationValidators;

namespace SierraSam.Core.Tests.Unit.MigrationValidators;

[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
internal sealed class RemoteMigrationValidatorTests
{
    private readonly IMigrationSeeker _migrationSeeker = Substitute.For<IMigrationSeeker>();
    private readonly IDatabase _database = Substitute.For<IDatabase>();
    private readonly IIgnoredMigrationsFactory _ignoredMigrationsFactory = Substitute.For<IIgnoredMigrationsFactory>();
    private readonly IMigrationValidator _validator = Substitute.For<IMigrationValidator>();

    private readonly IMigrationValidator _remoteMigrationValidator;

    public RemoteMigrationValidatorTests()
    {
        _remoteMigrationValidator = new RemoteMigrationValidator(
            _migrationSeeker,
            _database,
            _ignoredMigrationsFactory,
            _validator);
    }

    private static IEnumerable Constructor_with_null_args()
    {
        // ReSharper disable ObjectCreationAsStatement
        yield return new TestCaseData
            (new TestDelegate(() => new RemoteMigrationValidator(
                null!,
                Substitute.For<IDatabase>(),
                Substitute.For<IIgnoredMigrationsFactory>(),
                Substitute.For<IMigrationValidator>())))
            .SetName("null migration seeker");

        yield return new TestCaseData
            (new TestDelegate(() => new RemoteMigrationValidator(
                Substitute.For<IMigrationSeeker>(),
                null!,
                Substitute.For<IIgnoredMigrationsFactory>(),
                Substitute.For<IMigrationValidator>())))
            .SetName("null database");

        yield return new TestCaseData
            (new TestDelegate(() => new RemoteMigrationValidator(
                Substitute.For<IMigrationSeeker>(),
                Substitute.For<IDatabase>(),
                null!,
                Substitute.For<IMigrationValidator>())))
            .SetName("null ignored migrations factory");

        yield return new TestCaseData
            (new TestDelegate(() => new RemoteMigrationValidator(
                Substitute.For<IMigrationSeeker>(),
                Substitute.For<IDatabase>(),
                Substitute.For<IIgnoredMigrationsFactory>(),
                null!)))
            .SetName("null validator");
        // ReSharper enable ObjectCreationAsStatement
    }

    [TestCaseSource(nameof(Constructor_with_null_args))]
    public void Constructor_throws_for_null_args(TestDelegate constructor)
    {
        Assert.That(constructor, Throws.TypeOf<ArgumentNullException>());
    }

    [Test]
    public void Validate_returns_result_from_validator()
    {
        _validator
            .Validate()
            .Returns(1);

        _remoteMigrationValidator
            .Validate()
            .Should()
            .Be(1);
    }

    [TestCase(MigrationType.Any, MigrationState.Any)]
    [TestCase(MigrationType.Any, MigrationState.Missing)]
    public void Validate_short_circuits_when_appropriate_migrations_are_ignored(
        MigrationType type,
        MigrationState state)
    {
        _validator
            .Validate()
            .Returns(1);

        _ignoredMigrationsFactory
            .Create()
            .Returns(new[] { (type, state) });

        _remoteMigrationValidator
            .Validate()
            .Should()
            .Be(1);

        _database
            .DidNotReceive()
            .GetSchemaHistory();
    }

    [TestCase(MigrationType.Versioned, MigrationState.Missing)]
    [TestCase(MigrationType.Versioned, MigrationState.Any)]
    public void Validate_returns_when_versioned_migration_is_not_resolved_and_is_ignored(
        MigrationType type,
        MigrationState state)
    {
        _validator
            .Validate()
            .Returns(1);

        _ignoredMigrationsFactory
            .Create()
            .Returns(new[] { (type, state) });

        _migrationSeeker
            .Find()
            .Returns(Array.Empty<PendingMigration>());

        _database
            .GetSchemaHistory()
            .Returns(new []
            {
                new AppliedMigration(
                    1,
                    "1",
                    "someDescription",
                    "SQL",
                    "Filename.sql",
                    "someChecksum",
                    "someUser",
                    DateTime.UtcNow,
                    double.MinValue,
                    default)
            });

        _remoteMigrationValidator
            .Validate()
            .Should()
            .Be(1);
    }

    [TestCase(MigrationType.Repeatable, MigrationState.Missing)]
    [TestCase(MigrationType.Repeatable, MigrationState.Any)]
    public void Validate_returns_when_repeatable_migration_is_not_resolved_and_is_ignored(
        MigrationType type,
        MigrationState state)
    {
        _validator
            .Validate()
            .Returns(1);

        _ignoredMigrationsFactory
            .Create()
            .Returns(new[] { (type, state) });

        _migrationSeeker
            .Find()
            .Returns(Array.Empty<PendingMigration>());

        _database
            .GetSchemaHistory()
            .Returns(new []
            {
                new AppliedMigration(
                    1,
                    null,
                    "someDescription",
                    "SQL",
                    "Filename.sql",
                    "someChecksum",
                    "someUser",
                    DateTime.UtcNow,
                    double.MinValue,
                    default)
            });

        _remoteMigrationValidator
            .Validate()
            .Should()
            .Be(1);
    }

    [Test]
    public void Validate_throws_when_migration_has_miss_matching_checksum()
    {
        _validator
            .Validate()
            .Returns(1);

        _ignoredMigrationsFactory
            .Create()
            .Returns(Array.Empty<(MigrationType Type, MigrationState State)>());

        _migrationSeeker
            .Find()
            .Returns(new[]
            {
                new PendingMigration(
                    "1",
                    "description",
                    MigrationType.Versioned,
                    string.Empty,
                    "Filename.sql")
            });

        _database
            .GetSchemaHistory()
            .Returns(new []
            {
                new AppliedMigration(
                    1,
                    "1",
                    "someDescription",
                    "SQL",
                    "Filename.sql",
                    "miss-matched-checksum",
                    "someUser",
                    DateTime.UtcNow,
                    double.MinValue,
                    default)
            });

        _remoteMigrationValidator
            .Invoking(v => v.Validate())
            .Should()
            .Throw<Exception>()
            .WithMessage("Unable to find local migration Filename.sql");
    }

    [Test]
    public void Validate_throws_when_migration_is_not_resolved_and_not_ignored()
    {
        _validator
            .Validate()
            .Returns(1);

        _ignoredMigrationsFactory
            .Create()
            .Returns(Array.Empty<(MigrationType Type, MigrationState State)>());

        _migrationSeeker
            .Find()
            .Returns(Array.Empty<PendingMigration>());

        _database
            .GetSchemaHistory()
            .Returns(new []
            {
                new AppliedMigration(
                    1,
                    null,
                    "someDescription",
                    "SQL",
                    "Filename.sql",
                    "someChecksum",
                    "someUser",
                    DateTime.UtcNow,
                    double.MinValue,
                    default)
            });

        _remoteMigrationValidator
            .Invoking(v => v.Validate())
            .Should()
            .Throw<Exception>()
            .WithMessage("Unable to find local migration Filename.sql");
    }
}