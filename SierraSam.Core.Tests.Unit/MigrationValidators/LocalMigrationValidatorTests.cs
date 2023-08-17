using System.Collections;
using FluentAssertions;
using NSubstitute;
using SierraSam.Core.Enums;
using SierraSam.Core.MigrationValidators;

namespace SierraSam.Core.Tests.Unit.MigrationValidators;

internal sealed class LocalMigrationValidatorTests
{
    private static IEnumerable Constructor_with_null_args()
    {
        // ReSharper disable ObjectCreationAsStatement
        yield return new TestCaseData
            (new TestDelegate(() => new LocalMigrationValidator
                (null!, Substitute.For<IMigrationValidator>())))
            .SetName("null ignored migrations");

        yield return new TestCaseData
            (new TestDelegate(() => new LocalMigrationValidator
                (Array.Empty<(string, string)>(), null!)))
            .SetName("null validator");
        // ReSharper enable ObjectCreationAsStatement
    }

    [TestCaseSource(nameof(Constructor_with_null_args))]
    public void Constructor_throws_for_null_args(TestDelegate constructor)
    {
        Assert.That(constructor, Throws.TypeOf<ArgumentNullException>());
    }

    private static IEnumerable Validate_null_args()
    {
        yield return new TestCaseData(null, Array.Empty<PendingMigration>());
        yield return new TestCaseData(Array.Empty<AppliedMigration>(), null);
    }

    [TestCaseSource(nameof(Validate_null_args))]
    public void Validate_throws_for_null_args
        (IReadOnlyCollection<AppliedMigration> appliedMigrations,
         IReadOnlyCollection<PendingMigration> discoveredMigrations)
    {
        var sut = new LocalMigrationValidator
            (Array.Empty<(string, string)>(),
             Substitute.For<IMigrationValidator>());

        Assert.Throws<ArgumentNullException>(() => sut.Validate(appliedMigrations, discoveredMigrations));
    }

    [TestCase("pending")]
    [TestCase("*")]
    public void Validate_returns_execution_time_from_nested_validator_for_ignore_pattern
        (string status)
    {
        var appliedMigrations = Array.Empty<AppliedMigration>();
        var discoveredMigrations = Array.Empty<PendingMigration>();
        var executionTime = TimeSpan.FromMilliseconds(123);

        var nestedValidator = Substitute.For<IMigrationValidator>();

        nestedValidator
            .Validate(appliedMigrations, discoveredMigrations)
            .Returns(executionTime);

        var sut = new LocalMigrationValidator
            (new[] {("*", status)},
             nestedValidator);

        var result = sut.Validate(appliedMigrations, discoveredMigrations);

        Assert.That(result, Is.EqualTo(executionTime));
    }

    private static IEnumerable Ignored_migration_patterns()
    {
        yield return new TestCaseData
            (new[] { ("repeatable", "*"), ("versioned", "*") },
            new[]
            {
                CreatePendingMigration(MigrationType.Versioned),
                CreatePendingMigration(MigrationType.Repeatable)
            });

        yield return new TestCaseData
            (new[] { ("versioned", "*"), ("repeatable", "*") },
            new[]
            {
                CreatePendingMigration(MigrationType.Versioned),
                CreatePendingMigration(MigrationType.Repeatable)
            });

        yield return new TestCaseData
            (new[] { ("repeatable", "*") },
            new[]
            {
                CreatePendingMigration(MigrationType.Repeatable)
            });

        yield return new TestCaseData
            (new[] { ("versioned", "*") },
            new[]
            {
                CreatePendingMigration(MigrationType.Versioned)
            });
    }

    [TestCaseSource(nameof(Ignored_migration_patterns))]
    public void Validate_filters_discovered_migrations
        (IReadOnlyCollection<(string, string)> ignoredMigrations,
         PendingMigration[] discoveredMigrations)
    {
        var appliedMigrations = Array.Empty<AppliedMigration>();

        var nestedValidator = Substitute.For<IMigrationValidator>();

        var sut = new LocalMigrationValidator
            (ignoredMigrations,
             nestedValidator);

        sut.Validate(appliedMigrations, discoveredMigrations);

        Assert.Fail();
    }

    [Test]
    public void Validate_throws_when_discovered_migration_is_not_applied()
    {
        var appliedMigrations = Array.Empty<AppliedMigration>();
        var discoveredMigrations = new[] { CreatePendingMigration(MigrationType.Versioned) };

        var sut = new LocalMigrationValidator
            (Array.Empty<(string, string)>(),
             Substitute.For<IMigrationValidator>());

        sut
            .Invoking(v => v.Validate(appliedMigrations, discoveredMigrations))
            .Should()
            .Throw<Exception>()
            .WithMessage($"Unable to find remote migration {string.Empty}");
    }

    private static PendingMigration CreatePendingMigration(MigrationType migrationType)
        => new ("1", string.Empty, migrationType, string.Empty, string.Empty, string.Empty);
}