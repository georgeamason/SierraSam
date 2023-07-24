using System.Collections;
using System.Data.Odbc;
using System.IO.Abstractions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using SierraSam.Capabilities;
using SierraSam.Core;
using SierraSam.Core.MigrationSeekers;
using SierraSam.Database;

namespace SierraSam.Tests.Unit.Capabilities;

[TestFixture]
internal sealed class MigrateTests
{
    private static IEnumerable Constructors_with_null_arguments()
    {
        var configuration = new Configuration();

        // ReSharper disable ObjectCreationAsStatement
        yield return new TestCaseData
            (new TestDelegate(() => new Migrate
                (null!,
                 DatabaseFactory.Create(new OdbcConnection(), configuration),
                 configuration,
                 Substitute.For<IFileSystem>(),
                 Substitute.For<IMigrationSeeker>())))
            .SetName("Null logger");

        yield return new TestCaseData
            (new TestDelegate(() => new Migrate
                (Substitute.For<ILogger<Migrate>>(),
                 null!, 
                 configuration,
                 Substitute.For<IFileSystem>(),
                 Substitute.For<IMigrationSeeker>())))
            .SetName("Null ODBC connection");

        yield return new TestCaseData
            (new TestDelegate(() => new Migrate
                (Substitute.For<ILogger<Migrate>>(),
                 DatabaseFactory.Create(new OdbcConnection(), configuration), 
                 null!,
                 Substitute.For<IFileSystem>(),
                 Substitute.For<IMigrationSeeker>())))
            .SetName("Null configuration");

        yield return new TestCaseData
            (new TestDelegate(() => new Migrate
                (Substitute.For<ILogger<Migrate>>(),
                 DatabaseFactory.Create(new OdbcConnection(), configuration), 
                 new Configuration(),
                 null!,
                 Substitute.For<IMigrationSeeker>())))
            .SetName("Null file system");

        yield return new TestCaseData
            (new TestDelegate(() => new Migrate
            (Substitute.For<ILogger<Migrate>>(),
                DatabaseFactory.Create(new OdbcConnection(), configuration),
                new Configuration(),
                Substitute.For<IFileSystem>(),
                null!)))
            .SetName("Null migration seeker");
        // ReSharper restore ObjectCreationAsStatement
    }

    [TestCaseSource(nameof(Constructors_with_null_arguments))]
    public void Constructor_throws_for_null_args(TestDelegate constructor)
    {
        Assert.That(constructor, Throws.ArgumentNullException);
    }
}