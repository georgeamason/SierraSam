using System.Data.Odbc;
using DotNet.Testcontainers.Containers;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Respawn;
using Respawn.Graph;
using SierraSam.Capabilities;
using SierraSam.Core;
using SierraSam.Core.Enums;
using SierraSam.Core.Extensions;
using SierraSam.Core.Factories;
using SierraSam.Core.MigrationApplicators;
using SierraSam.Core.MigrationSeekers;
using SierraSam.Database;
using Spectre.Console.Testing;
using static SierraSam.Tests.Integration.DbContainerFactory;

namespace SierraSam.Tests.Integration.Capabilities;

[TestFixture]
internal sealed class MigrateTests
{
    private readonly ILogger<Migrate> _logger = Substitute.For<ILogger<Migrate>>();
    private readonly TestConsole _console = new ();

    private static readonly IEnumerable<TestCaseData> ContainerTestCases = new[]
    {
        new TestCaseData(new SqlServer("2022-latest")).SetName("SqlServer-2022"),
        new TestCaseData(new SqlServer("2017-latest")).SetName("SqlServer-2017"),
        new TestCaseData(new Postgres("16")).SetName("Postgres-16"),
        new TestCaseData(new Postgres("15")).SetName("Postgres-15"),
        new TestCaseData(new MySql("8.2")).SetName("MySql-8.2"),
        new TestCaseData(new MySql("5.7")).SetName("MySql-5.7")
    };

    [SetUp]
    public async Task SetUp()
    {
        var container = TestContext.CurrentContext.Test.Arguments[0].As<IDbContainer>();

        await container.StartAsync();
    }

    [TestCaseSource(nameof(ContainerTestCases))]
    public void Migrate_creates_schema_history_when_not_initialized(IDbContainer container)
    {
        var configuration = new Configuration(url: container.DbConnection.ConnectionString);

        using var odbcConnection = OdbcConnectionFactory.Create(_logger, configuration);

        var database = DatabaseResolver.Create(
            new NullLoggerFactory(),
            odbcConnection,
            new DbExecutor(odbcConnection),
            configuration,
            new MemoryCache(new MemoryCacheOptions())
        );

        var migrationSeeker = Substitute.For<IMigrationSeeker>();

        var migrationApplicator = Substitute.For<IMigrationsApplicator>();

        var sut = new Migrate(
            _logger,
            database,
            configuration,
            migrationSeeker,
            migrationApplicator,
            _console
        );

        sut.Run(Array.Empty<string>());

        database.HasMigrationTable.Should().BeTrue();
        database.GetSchemaHistory().Should().BeEquivalentTo(Array.Empty<AppliedMigration>());
    }

    [TestCaseSource(nameof(ContainerTestCases))]
    public void Migrate_applies_pending_migrations(IDbContainer container)
    {
        var configuration = new Configuration(
            url: container.DbConnection.ConnectionString,
            installedBy: "SierraSam"
        );

        using var odbcConnection = OdbcConnectionFactory.Create(_logger, configuration);

        var database = DatabaseResolver.Create(
            new NullLoggerFactory(),
            odbcConnection,
            new DbExecutor(odbcConnection),
            configuration,
            new MemoryCache(new MemoryCacheOptions())
        );

        var migrationSeeker = Substitute.For<IMigrationSeeker>();

        migrationSeeker.Find().Returns(new PendingMigration[]
        {
            new ("1", "Add table foo", MigrationType.Versioned, "CREATE TABLE foo (id INT);", string.Empty),
            // new (null, "Write into foo", MigrationType.Repeatable, "INSERT INTO foo VALUES (1);", string.Empty),
        });

        var migrationApplicator = new MigrationsApplicator(
            database,
            new MigrationApplicatorResolver(new IMigrationApplicator[]
            {
                new RepeatableMigrationApplicator(
                    database,
                    configuration,
                    _console
                ),
                new VersionedMigrationApplicator(
                    database,
                    configuration,
                    _console
                )
            })
        );

        var sut = new Migrate(
            _logger,
            database,
            configuration,
            migrationSeeker,
            migrationApplicator,
            _console
        );

        sut.Run(Array.Empty<string>());

        database.HasMigrationTable.Should().BeTrue();

        database.GetSchemaHistory().Should().BeEquivalentTo(new AppliedMigration[]
            {
                new(1,
                    "1",
                    "Add table foo",
                    "SQL",
                    string.Empty,
                    "CREATE TABLE foo (id INT);".Checksum(),
                    "SierraSam",
                    DateTime.UtcNow,
                    default,
                    true)
            },
            options => options
                .Excluding(migration => migration.ExecutionTime)
                .Excluding(migration => migration.InstalledOn)
        );
    }

    [TearDown]
    public async Task CleanDatabase()
    {
        var container = TestContext.CurrentContext.Test.Arguments[0].As<IDbContainer>();

        await using var connection = new OdbcConnection(
            container.DbConnection.ConnectionString
        );

        await connection.OpenAsync();

        var respawner = await Respawner.CreateAsync(
            connection,
            new RespawnerOptions
            {
                DbAdapter = container.Adapter
            }
        );

        await respawner.ResetAsync(connection);
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        var containers = ContainerTestCases.Select(data => data.Arguments[0].As<IDbContainer>());
        foreach (var container in containers.Where(container => container.State is TestcontainersStates.Running))
        {
            await container.StopAsync();
        }
    }
}