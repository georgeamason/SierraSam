using System.Collections;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using SierraSam.Capabilities;
using SierraSam.Core;
using SierraSam.Core.Enums;
using SierraSam.Core.Extensions;
using SierraSam.Core.MigrationSeekers;
using SierraSam.Core.MigrationValidators;
using Spectre.Console;
using Spectre.Console.Testing;

namespace SierraSam.Tests.Unit.Capabilities;

[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
internal sealed class MigrateTests
{
    private readonly ILogger<Migrate> _logger = Substitute.For<ILogger<Migrate>>();
    private readonly IDatabase _database = Substitute.For<IDatabase>();
    private readonly IConfiguration _configuration = Substitute.For<IConfiguration>();
    private readonly IMigrationValidator _validator = Substitute.For<IMigrationValidator>();
    private readonly IMigrationSeeker _migrationSeeker = Substitute.For<IMigrationSeeker>();
    private readonly IMigrationsApplicator _migrationsApplicator = Substitute.For<IMigrationsApplicator>();
    private readonly TestConsole _console = new();
    private readonly Migrate _sut;

    public MigrateTests()
    {
        _sut = new Migrate(
            _logger,
            _database,
            _configuration,
            _validator,
            _migrationSeeker,
            _migrationsApplicator,
            _console
        );
    }

    private static IEnumerable Constructors_with_null_arguments()
    {
        // ReSharper disable ObjectCreationAsStatement
        yield return new TestCaseData
            (new TestDelegate(() => new Migrate
                (null!,
                 Substitute.For<IDatabase>(),
                 Substitute.For<IConfiguration>(),
                 Substitute.For<IMigrationValidator>(),
                 Substitute.For<IMigrationSeeker>(),
                 Substitute.For<IMigrationsApplicator>(),
                 Substitute.For<IAnsiConsole>())))
            .SetName("Null logger");

        yield return new TestCaseData
            (new TestDelegate(() => new Migrate
                (Substitute.For<ILogger<Migrate>>(),
                 null!, 
                 Substitute.For<IConfiguration>(),
                 Substitute.For<IMigrationValidator>(),
                 Substitute.For<IMigrationSeeker>(),
                 Substitute.For<IMigrationsApplicator>(),
                 Substitute.For<IAnsiConsole>())))
            .SetName("Null database");

        yield return new TestCaseData
            (new TestDelegate(() => new Migrate
                (Substitute.For<ILogger<Migrate>>(),
                 Substitute.For<IDatabase>(),
                 null!,
                 Substitute.For<IMigrationValidator>(),
                 Substitute.For<IMigrationSeeker>(),
                 Substitute.For<IMigrationsApplicator>(),
                 Substitute.For<IAnsiConsole>())))
            .SetName("Null configuration");

        yield return new TestCaseData(
                new TestDelegate(() => new Migrate(
                    Substitute.For<ILogger<Migrate>>(),
                    Substitute.For<IDatabase>(),
                    Substitute.For<IConfiguration>(),
                    null!,
                    Substitute.For<IMigrationSeeker>(),
                    Substitute.For<IMigrationsApplicator>(),
                    Substitute.For<IAnsiConsole>()
                ))
            )
            .SetName("Null validator");

        yield return new TestCaseData
            (new TestDelegate(() => new Migrate
                (Substitute.For<ILogger<Migrate>>(),
                 Substitute.For<IDatabase>(),
                 Substitute.For<IConfiguration>(),
                 Substitute.For<IMigrationValidator>(),
                 null!,
                 Substitute.For<IMigrationsApplicator>(),
                 Substitute.For<IAnsiConsole>())))
            .SetName("Null migration seeker");

        yield return new TestCaseData
            (new TestDelegate(() => new Migrate
                (Substitute.For<ILogger<Migrate>>(),
                 Substitute.For<IDatabase>(),
                 Substitute.For<IConfiguration>(),
                 Substitute.For<IMigrationValidator>(),
                 Substitute.For<IMigrationSeeker>(),
                 null!,
                 Substitute.For<IAnsiConsole>())))
            .SetName("Null migration applicator");

        yield return new TestCaseData
            (new TestDelegate(() => new Migrate
                (Substitute.For<ILogger<Migrate>>(),
                Substitute.For<IDatabase>(),
                Substitute.For<IConfiguration>(),
                Substitute.For<IMigrationValidator>(),
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

    [Test]
    public void If_database_has_no_migration_table_then_it_is_created()
    {
        _database.HasMigrationTable().Returns(false);

        _sut.Run(Array.Empty<string>());

        _database.Received().CreateSchemaHistory();
    }

    [Test]
    public void Applicator_is_called_with_expected_pending_migrations()
    {
        _database.HasMigrationTable().Returns(true);

        _migrationSeeker.GetPendingMigrations().Returns(new PendingMigration[]
        {
            new("1", "description", MigrationType.Versioned, string.Empty, string.Empty),
            new(null, "someDescriptionB", MigrationType.Repeatable, string.Empty, string.Empty),
            new("2", "anotherDescription", MigrationType.Versioned, string.Empty, string.Empty),
            new(null, "someDescriptionA", MigrationType.Repeatable, string.Empty, string.Empty),
        });

        _database.GetAppliedMigrations().Returns(new AppliedMigration[]
        {
            new(1,
                "1",
                "description",
                "SQL",
                "someScript",
                string.Empty.Checksum(),
                "someUser",
                DateTime.UtcNow,
                double.MinValue,
                true)
        });

        _migrationsApplicator
            .Apply(Arg.Any<IEnumerable<PendingMigration>>(), out _)
            .Returns(1);

        IEnumerable<PendingMigration> migrationsToApply = null!;

        _migrationsApplicator
            .When(applicator => applicator.Apply(Arg.Any<IEnumerable<PendingMigration>>(), out _))
            .Do(info => migrationsToApply = info.Arg<IEnumerable<PendingMigration>>());

        _sut.Run(Array.Empty<string>());

        _console.Output.Should().Contain("Current version of schema \"\": 1");

        migrationsToApply.Should().BeEquivalentTo(new PendingMigration[]
        {
            new("2", "anotherDescription", MigrationType.Versioned, string.Empty, string.Empty),
            new(null, "someDescriptionA", MigrationType.Repeatable, string.Empty, string.Empty),
            new(null, "someDescriptionB", MigrationType.Repeatable, string.Empty, string.Empty),
        }, options => options.WithStrictOrdering());

        _console.Output.Should().Contain("Successfully applied 1 migration(s) to schema \"\"");
    }
}