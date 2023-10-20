using FluentAssertions;
using NSubstitute;
using SierraSam.Core.MigrationApplicators;

namespace SierraSam.Core.Tests.Unit.MigrationApplicators;

internal sealed class MigrationApplicatorResolverTests
{
    [Test]
    public void Constructor_throws_for_null_args()
    {
        Assert.That(() => new MigrationApplicatorResolver(null!), Throws.ArgumentNullException);
    }

    [Test]
    public void Resolve_returns_migration_applicator()
    {
        var applicator1 = Substitute.For<IMigrationApplicator>();

        var sut = new MigrationApplicatorResolver(new[] { applicator1 });

        sut.Resolve(applicator1.GetType()).Should().Be(applicator1);
    }

    [Test]
    public void Resolve_throws_when_applicator_is_not_registered()
    {
        var sut = new MigrationApplicatorResolver(Array.Empty<IMigrationApplicator>());

        sut.Invoking(resolver => resolver.Resolve(typeof(string)))
            .Should()
            .Throw<ArgumentException>()
            .WithMessage("* is not a registered migration applicator");
    }
}