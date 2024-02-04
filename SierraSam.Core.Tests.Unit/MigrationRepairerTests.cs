using System.Data;
using System.Data.Odbc;
using NSubstitute;
using SierraSam.Core.Enums;
using SierraSam.Core.Extensions;

namespace SierraSam.Core.Tests.Unit;

[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
internal sealed class MigrationRepairerTests
{
    private readonly IDatabase _database = Substitute.For<IDatabase>();
    private readonly MigrationRepairer _sut;

    public MigrationRepairerTests()
    {
        _sut = new MigrationRepairer(_database);
    }

    [Test]
    public void Repair_WhenCalled_UpdatesSchemaHistory()
    {
        var toRepair = new AppliedMigration(
            1,
            "1",
            "someDescription",
            "someType",
            "someScript",
            "someChecksum",
            "someUser",
            DateTime.UtcNow,
            double.MinValue,
            true
        );

        var repairWith = new PendingMigration(
            "1",
            "anotherDescription",
            MigrationType.Versioned,
            "someSql",
            "someFilename"
        );

        var repairs = new Dictionary<AppliedMigration, PendingMigration>
        {
            { toRepair, repairWith }
        };

        _sut.Repair(repairs);

        _database
            .Received(1)
            .UpdateSchemaHistory(
                Arg.Is<AppliedMigration>(m =>
                    m.Version == "1" &&
                    m.Script == "someScript" &&
                    m.InstalledBy == "someUser" &&
                    m.Description == "anotherDescription" &&
                    m.Checksum == "someSql".Checksum()
                ),
                Arg.Any<IDbTransaction>()
            );
    }
}