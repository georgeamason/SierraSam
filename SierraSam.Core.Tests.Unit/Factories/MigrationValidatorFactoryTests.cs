using System.Collections;
using System.IO.Abstractions;
using NSubstitute;
using SierraSam.Core.Factories;
using SierraSam.Core.MigrationValidators;

namespace SierraSam.Core.Tests.Unit.Factories;

internal sealed class MigrationValidatorFactoryTests
{
    private static IEnumerable Create_with_null_args()
    {
        yield return new TestCaseData
            (new TestDelegate(() => MigrationValidatorFactory.Create
                (null!)))
            .SetName("null configuration");
    }

    [TestCaseSource(nameof(Create_with_null_args))]
    public void Create_throws_for_null_args(TestDelegate constructor)
    {
        Assert.That(constructor, Throws.TypeOf<ArgumentNullException>());
    }

    [Test]
    public void Create_returns_a_local_migration_validator()
    {
        var configuration = new Configuration();

        var migrationValidator = MigrationValidatorFactory.Create(configuration);

        Assert.That(migrationValidator, Is.TypeOf<LocalMigrationValidator>());
    }

    [TestCase("bad pattern")]
    [TestCase("bad pattern:")]
    [TestCase(":bad pattern")]
    [TestCase(":bad pattern:")]
    [TestCase("")]
    public void Create_does_not_throw_for_bad_ignore_pattern(string badPattern)
    {
        var configuration = new Configuration(ignoredMigrations: new []{badPattern});

        Assert.DoesNotThrow(() => MigrationValidatorFactory.Create(configuration));
    }
}