using System.IO.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NUnit.Framework;
using SierraSam.Capabilities;
using SierraSam.Core;
using SierraSam.Core.Extensions;
using SierraSam.Core.MigrationSeekers;
using SierraSam.Core.MigrationValidators;
using Spectre.Console;
using static SierraSam.Core.Enums.MigrationType;

namespace SierraSam.Tests.Unit.Capabilities;

[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
internal sealed class RollupTests
{
    private readonly ILogger<Rollup> _logger = NullLogger<Rollup>.Instance;
    private readonly IDatabase _database = Substitute.For<IDatabase>();
    private readonly IConfiguration _configuration = Substitute.For<IConfiguration>();
    private readonly IMigrationValidator _validator = Substitute.For<IMigrationValidator>();
    private readonly IMigrationSeeker _migrationSeeker = Substitute.For<IMigrationSeeker>();
    private readonly IFileSystem _fileSystem = Substitute.For<IFileSystem>();
    private readonly IAnsiConsole _console = Substitute.For<IAnsiConsole>();
    private readonly Rollup _sut;

    public RollupTests()
    {
        _sut = new Rollup(
            _logger,
            _database,
            _configuration,
            _validator,
            _migrationSeeker,
            _fileSystem,
            _console
        );
    }

    [Test]
    public void Rollup_squashes_discovered_migrations_into_file()
    {
        _database.GetAppliedMigrations().Returns(new AppliedMigration[]
        {
            new (1,
                "1",
                "someDescription",
                "SQL",
                "V1__someDescription.sql",
                "sql1".Checksum(),
                "someUser",
                DateTime.UtcNow,
                double.MinValue,
                true),
            new (2,
                "2",
                "anotherDescription",
                "SQL",
                "V2__anotherDescription.sql",
                "sql2".Checksum(),
                "someUser",
                DateTime.UtcNow,
                double.MinValue,
                true),
        });

        _migrationSeeker.GetPendingMigrations().Returns(new PendingMigration[]
        {
            new ("1", "someDescription", Versioned, "sql1", "V1__someDescription.sql"),
            new ("2", "anotherDescription", Versioned, "sql2", "V2__anotherDescription.sql"),
        });

        _configuration.ExportDirectory.Returns("/tmp");

        _sut.Run(Array.Empty<string>());

        _fileSystem.Received().File.WriteAllText(
            "/tmp/rollup.sql",
            """
            --V1__someDescription.sql--
            sql1

            --V2__anotherDescription.sql--
            sql2
            """
        );
    }
}