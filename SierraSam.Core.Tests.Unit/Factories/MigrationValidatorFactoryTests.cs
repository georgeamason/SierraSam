using System.Collections;
using NSubstitute;
using SierraSam.Core.Factories;
using SierraSam.Core.MigrationSeekers;
using SierraSam.Core.MigrationValidators;

namespace SierraSam.Core.Tests.Unit.Factories;

internal sealed class MigrationValidatorFactoryTests
{
    private static IEnumerable Create_with_null_args()
    {
        yield return new TestCaseData
            (new TestDelegate(() => MigrationValidatorFactory.Create(
                null!,
                Substitute.For<IDatabase>(),
                Substitute.For<IIgnoredMigrationsFactory>())))
            .SetName("null migration seeker");

        yield return new TestCaseData
            (new TestDelegate(() => MigrationValidatorFactory.Create(
                Substitute.For<IMigrationSeeker>(),
                null!,
                Substitute.For<IIgnoredMigrationsFactory>())))
            .SetName("null database");

        yield return new TestCaseData
            (new TestDelegate(() => MigrationValidatorFactory.Create(
                Substitute.For<IMigrationSeeker>(),
                Substitute.For<IDatabase>(),
                null!)))
            .SetName("null ignored migrations factory");
    }

    [TestCaseSource(nameof(Create_with_null_args))]
    public void Create_throws_for_null_args(TestDelegate constructor)
    {
        Assert.That(constructor, Throws.TypeOf<ArgumentNullException>());
    }
}