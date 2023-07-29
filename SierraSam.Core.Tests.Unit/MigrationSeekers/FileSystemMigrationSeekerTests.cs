using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using FluentAssertions;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using NSubstitute;
using SierraSam.Core.MigrationSeekers;

namespace SierraSam.Core.Tests.Unit.MigrationSeekers;

internal sealed class FileSystemMigrationSeekerTests
{
    [SuppressMessage("ReSharper", "ObjectCreationAsStatement")]
    private static IEnumerable Initialisation_with_null_args()
    {
        yield return new TestCaseData
            (new TestDelegate
                (() => new FileSystemMigrationSeeker(null!, Substitute.For<IFileSystem>())))
            .SetName("Null configuration");

        yield return new TestCaseData
            (new TestDelegate
                (() => new FileSystemMigrationSeeker(new Configuration(), null!)))
            .SetName("Null file system");
    }

    [TestCaseSource(nameof(Initialisation_with_null_args))]
    public void Constructor_throws_for_null_args(TestDelegate constructor)
    {
        constructor
            .Invoking(c => c.Invoke())
            .Should()
            .Throw<ArgumentNullException>();
    }
}