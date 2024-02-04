using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using SierraSam.Capabilities;
using SierraSam.Core;
using SierraSam.Core.Enums;
using SierraSam.Core.Extensions;
using SierraSam.Core.MigrationSeekers;
using Spectre.Console;

namespace SierraSam.Tests.Unit.Capabilities;

[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
internal sealed class RepairTests
{
    private readonly ILogger<Repair> _logger = Substitute.For<ILogger<Repair>>();
    private readonly IMigrationSeeker _migrationSeeker = Substitute.For<IMigrationSeeker>();
    private readonly IDatabase _database = Substitute.For<IDatabase>();
    private readonly IAnsiConsole _console = Substitute.For<IAnsiConsole>();
    private readonly IConfiguration _configuration = Substitute.For<IConfiguration>();
    private readonly IMigrationRepairer _repairer = Substitute.For<IMigrationRepairer>();
    private readonly Repair _sut;

    public RepairTests()
    {
        _sut = new Repair(_logger, _migrationSeeker, _database, _console, _configuration, _repairer);
    }

    [Test]
    public void Run_repairs_expected_migrations()
    {
        _migrationSeeker.GetPendingMigrations().Returns(new PendingMigration[]
        {
            new("1",
                "someDescription",
                MigrationType.Versioned,
                "someSql",
                "someFilename"
            ),
            new("2",
                "someOtherDescription",
                MigrationType.Versioned,
                "someOtherSql",
                "someOtherFilename"
            )
        });

        _database.GetAppliedMigrations().Returns(new AppliedMigration[]
        {
            new(1,
                "1",
                "anotherDescription",
                "someType",
                "someScript",
                "someChecksum",
                "someUser",
                new DateTime(2024, 02, 28, 00, 00, 00, DateTimeKind.Utc),
                double.MinValue,
                true
            ),
            new(2,
                "2",
                "someOtherDescription",
                "someType",
                "someOtherScript",
                "someOtherSql".Checksum(),
                "someUser",
                DateTime.UtcNow,
                double.MinValue,
                true
            )
        });

        IDictionary<AppliedMigration, PendingMigration> repairs = null!;

        _repairer
            .When(repairer => repairer.Repair(Arg.Any<IDictionary<AppliedMigration, PendingMigration>>()))
            .Do(info => repairs = info.Arg<IDictionary<AppliedMigration,PendingMigration>>());

        _sut.Run(Array.Empty<string>());

        repairs.Should().BeEquivalentTo(new Dictionary<AppliedMigration, PendingMigration>
        {
            {
                new(1,
                    "1",
                    "anotherDescription",
                    "someType",
                    "someScript",
                    "someChecksum",
                    "someUser",
                    new DateTime(2024, 02, 28, 00, 00, 00, DateTimeKind.Utc),
                    double.MinValue,
                    true
                ),
                new("1",
                    "someDescription",
                    MigrationType.Versioned,
                    "someSql",
                    "someFilename"
                )
            }
        });
    }
}