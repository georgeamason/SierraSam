using FluentAssertions;
using NSubstitute;
using SierraSam.Core.Exceptions;
using SierraSam.Core.MigrationSeekers;
using SierraSam.Core.MigrationValidators;
using static SierraSam.Core.Enums.MigrationType;

namespace SierraSam.Core.Tests.Unit.MigrationValidators;

[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
internal sealed class DistinctVersionMigrationValidatorTests
{
    private readonly IMigrationSeeker _migrationSeeker = Substitute.For<IMigrationSeeker>();
    private readonly IMigrationValidator _migrationValidator = Substitute.For<IMigrationValidator>();
    private readonly DistinctVersionMigrationValidator _sut;

    public DistinctVersionMigrationValidatorTests() => _sut = new(_migrationSeeker, _migrationValidator);

    [Test]
    public void Validate_throws_when_for_migrations_with_the_same_version()
    {
        _migrationSeeker.Find().Returns(new[]
        {
            new PendingMigration("1", "someDescription", Versioned, string.Empty, "someFilename"),
            new PendingMigration("1", "anotherDescription", Versioned, string.Empty, "anotherFilename")
        });

        _sut.Invoking(validator => validator.Validate())
            .Should()
            .Throw<MigrationValidatorException>()
            .WithMessage("Discovered multiple migrations with version 1");
    }

    [Test]
    public void Validate_returns_count_of_migrations_with_distinct_versions()
    {
        _migrationValidator.Validate().Returns(2);

        _migrationSeeker.Find().Returns(new[]
        {
            new PendingMigration("1", "someDescription", Versioned, string.Empty, "someFilename"),
            new PendingMigration("2", "anotherDescription", Versioned, string.Empty, "anotherFilename"),
            new PendingMigration(null, "someOtherDescription", Repeatable, string.Empty, "someOtherFilename")
        });

        _sut.Validate().Should().Be(2);
    }
}

