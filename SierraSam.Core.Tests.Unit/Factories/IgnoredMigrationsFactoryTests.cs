using System.Collections;
using FluentAssertions;
using NSubstitute;
using SierraSam.Core.Enums;
using SierraSam.Core.Factories;
using static SierraSam.Core.Enums.MigrationState;
using static SierraSam.Core.Enums.MigrationType;

namespace SierraSam.Core.Tests.Unit.Factories;

[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
internal class IgnoredMigrationsFactoryTests
{
    private readonly IConfiguration _configuration = Substitute.For<IConfiguration>();
    private readonly IgnoredMigrationsFactory _sut;

    public IgnoredMigrationsFactoryTests() => _sut = new(_configuration);

    [Test]
    public void Constructor_throws_for_null_args()
    {
        // ReSharper disable ObjectCreationAsStatement
        var act = () => new IgnoredMigrationsFactory(null!);
        // ReSharper restore ObjectCreationAsStatement

        act.Should().Throw<ArgumentNullException>();
    }

    private static IEnumerable IgnoredMigrationsTestCases()
    {
        yield return new TestCaseData(
            new[] { "versioned:pending", "versioned:applied", "versioned:missing", },
            new[] { (Versioned, Pending), (Versioned, Applied), (Versioned, Missing), }
        );

        yield return new TestCaseData(
            new[] { "repeatable:pending", "repeatable:applied", "repeatable:missing", },
            new[] { (Repeatable, Pending), (Repeatable, Applied), (Repeatable, Missing), }
        );

        yield return new TestCaseData(
            new[] { "*:pending", },
            new[] { (MigrationType.Any, Pending), }
        );

        yield return new TestCaseData(
            new[] { "versioned:*", },
            new[] { (Versioned, MigrationState.Any), }
        );

        yield return new TestCaseData(
            Array.Empty<string>(),
            Array.Empty<(MigrationType type, MigrationState state)>()
        );
    }

    [TestCaseSource(nameof(IgnoredMigrationsTestCases))]
    public void Create_returns_expected_results(
        IEnumerable<string> migrationsToIgnore,
        IEnumerable<(MigrationType type, MigrationState state)> expectation
    )
    {
        _configuration.IgnoredMigrations = migrationsToIgnore;

        _sut.Create().Should().BeEquivalentTo(expectation);
    }

    [TestCase("type:state")]
    [TestCase("bad")]
    [TestCase("fake::news")]
    [TestCase("one:two:three")]
    [TestCase(" : ")]
    [TestCase("versioned:bad")]
    [TestCase("fake:pending")]
    public void Create_throws_for_invalid_pattern(string payload)
    {
        _configuration.IgnoredMigrations = new[] { payload };

        var sut = new IgnoredMigrationsFactory(_configuration);

        sut.Invoking(factory => factory.Create()).Should().Throw<ArgumentException>();
    }

    [TestCase("Versioned:PENDING")]
    [TestCase("REPEATABLE:Pending")]
    public void Create_ignores_casing(string payload)
    {
        _configuration.IgnoredMigrations = new []{ payload };

        _sut.Invoking(factory => factory.Create()).Should().NotThrow();
    }

    [Test]
    public void Create_ignores_duplicates()
    {
        _configuration.IgnoredMigrations = new []{ "versioned:pending", "versioned:pending" };

        _sut.Create().Should().BeEquivalentTo(new[] { (Versioned, Pending) });
    }

    [Test]
    public void Create_ignores_empty_and_whitespace_patterns_and_trims()
    {
        _configuration.IgnoredMigrations = new []{ " versioned:pending ", "", "   " };

        _sut.Create().Should().BeEquivalentTo(new[] { (Versioned, Pending) });
    }
}