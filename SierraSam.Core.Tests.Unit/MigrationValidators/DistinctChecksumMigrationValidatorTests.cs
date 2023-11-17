using FluentAssertions;
using NSubstitute;
using SierraSam.Core.Exceptions;
using SierraSam.Core.MigrationSeekers;
using SierraSam.Core.MigrationValidators;
using static SierraSam.Core.Enums.MigrationType;

namespace SierraSam.Core.Tests.Unit.MigrationValidators;

[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
internal sealed class DistinctChecksumMigrationValidatorTests
{
    private readonly IMigrationSeeker _migrationSeeker = Substitute.For<IMigrationSeeker>();
    private readonly DistinctChecksumMigrationValidator _sut;

    public DistinctChecksumMigrationValidatorTests() => _sut = new(_migrationSeeker);

    [Test]
    public void Constructor_throws_for_null_args()
    {
        var act = () => new DistinctChecksumMigrationValidator(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Test]
    public void Validate_throws_when_for_non_distinct_migrations()
    {
        const string sql = "SELECT 1";

        _migrationSeeker.Find().Returns(new[]
        {
            new PendingMigration("1", "someDescription", Versioned, sql, "someFilename"),
            new PendingMigration("2", "anotherDescription", Versioned, sql, "anotherFilename")
        });

        _sut.Invoking(validator => validator.Validate())
            .Should()
            .Throw<MigrationValidatorException>()
            .WithMessage("Discovered multiple migrations with equal contents");
    }

    [Test]
    public void Validate_returns_count_of_distinct_migrations()
    {
        _migrationSeeker.Find().Returns(new[]
        {
            new PendingMigration("1", "someDescription", Versioned, "SELECT 1", "someFilename"),
            new PendingMigration("2", "anotherDescription", Versioned, "SELECT 2", "anotherFilename")
        });

        _sut.Validate().Should().Be(2);
    }
}