using System.Collections;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using SierraSam.Capabilities;
using SierraSam.Core;
using SierraSam.Core.MigrationSeekers;
using Spectre.Console;

namespace SierraSam.Tests.Unit.Capabilities;

[TestFixture]
internal sealed class MigrateTests
{
    private static IEnumerable Constructors_with_null_arguments()
    {
        // ReSharper disable ObjectCreationAsStatement
        yield return new TestCaseData
            (new TestDelegate(() => new Migrate
                (null!,
                 Substitute.For<IDatabase>(),
                 Substitute.For<IConfiguration>(),
                 Substitute.For<IMigrationSeeker>(),
                 Substitute.For<IMigrationsApplicator>(),
                 Substitute.For<IAnsiConsole>())))
            .SetName("Null logger");

        yield return new TestCaseData
            (new TestDelegate(() => new Migrate
                (Substitute.For<ILogger<Migrate>>(),
                 null!, 
                 Substitute.For<IConfiguration>(),
                 Substitute.For<IMigrationSeeker>(),
                 Substitute.For<IMigrationsApplicator>(),
                 Substitute.For<IAnsiConsole>())))
            .SetName("Null database");

        yield return new TestCaseData
            (new TestDelegate(() => new Migrate
                (Substitute.For<ILogger<Migrate>>(),
                 Substitute.For<IDatabase>(),
                 null!,
                 Substitute.For<IMigrationSeeker>(),
                 Substitute.For<IMigrationsApplicator>(),
                 Substitute.For<IAnsiConsole>())))
            .SetName("Null configuration");

        yield return new TestCaseData
            (new TestDelegate(() => new Migrate
                (Substitute.For<ILogger<Migrate>>(),
                 Substitute.For<IDatabase>(),
                 Substitute.For<IConfiguration>(),
                 null!,
                 Substitute.For<IMigrationsApplicator>(),
                 Substitute.For<IAnsiConsole>())))
            .SetName("Null migration seeker");

        yield return new TestCaseData
            (new TestDelegate(() => new Migrate
                (Substitute.For<ILogger<Migrate>>(),
                 Substitute.For<IDatabase>(),
                 Substitute.For<IConfiguration>(),
                 Substitute.For<IMigrationSeeker>(),
                 null!,
                 Substitute.For<IAnsiConsole>())))
            .SetName("Null migration applicator");

        yield return new TestCaseData
            (new TestDelegate(() => new Migrate
                (Substitute.For<ILogger<Migrate>>(),
                Substitute.For<IDatabase>(),
                Substitute.For<IConfiguration>(),
                Substitute.For<IMigrationSeeker>(),
                Substitute.For<IMigrationsApplicator>(),
                null!)))
            .SetName("Null console");
        // ReSharper restore ObjectCreationAsStatement
    }

    [TestCaseSource(nameof(Constructors_with_null_arguments))]
    public void Constructor_throws_for_null_args(TestDelegate constructor)
    {
        Assert.That(constructor, Throws.ArgumentNullException);
    }
}