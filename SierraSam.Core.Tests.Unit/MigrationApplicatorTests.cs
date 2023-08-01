using System.Collections;
using System.IO.Abstractions;
using NSubstitute;

namespace SierraSam.Core.Tests.Unit;

internal sealed class MigrationApplicatorTests
{
    private static IEnumerable Constructors_with_null_args()
    {
        // ReSharper disable ObjectCreationAsStatement
        yield return new TestCaseData
            (new TestDelegate
                (() => new MigrationApplicator
                    (null!,
                     Substitute.For<IFileSystem>(),
                     Substitute.For<Configuration>())))
            .SetName("null database");

        yield return new TestCaseData
            (new TestDelegate
                (() => new MigrationApplicator
                    (Substitute.For<IDatabase>(),
                     null!,
                     Substitute.For<Configuration>())))
            .SetName("null filesystem");

        yield return new TestCaseData
            (new TestDelegate
                (() => new MigrationApplicator
                    (Substitute.For<IDatabase>(),
                     Substitute.For<IFileSystem>(),
                     null!)))
            .SetName("null configuration");
        // ReSharper enable ObjectCreationAsStatement
    }

    [TestCaseSource(nameof(Constructors_with_null_args))]
    public void Constructor_throws_for_null_arguments(TestDelegate constructor)
    {
        Assert.Throws<ArgumentNullException>(constructor);
    }
}