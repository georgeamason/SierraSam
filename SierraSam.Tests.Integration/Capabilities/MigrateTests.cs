using DotNet.Testcontainers.Containers;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using SierraSam.Capabilities;
using SierraSam.Core;
using SierraSam.Core.Extensions;
using SierraSam.Core.MigrationApplicators;
using SierraSam.Core.MigrationSeekers;
using SierraSam.Database;
using Spectre.Console.Testing;
using static SierraSam.Core.Enums.MigrationType;
using static SierraSam.Tests.Integration.DbContainerFactory;

namespace SierraSam.Tests.Integration.Capabilities;

[TestFixtureSource(typeof(SqlServer), nameof(SqlServer.TestCases))]
[TestFixtureSource(typeof(Postgres), nameof(Postgres.TestCases))]
[TestFixtureSource(typeof(MySql), nameof(MySql.TestCases))]
[TestFixtureSource(typeof(Oracle), nameof(Oracle.TestCases))]
internal sealed class MigrateTests
{
    private readonly ITestContainer _testContainer;
    private readonly ILogger<Migrate> _logger = Substitute.For<ILogger<Migrate>>();
    private readonly TestConsole _console = new ();

    public MigrateTests(ITestContainer testContainer) => _testContainer = testContainer;

    [SetUp]
    public Task SetUp()
    {
        // ReSharper disable once ConvertIfStatementToReturnStatement
        if (_testContainer.State is not TestcontainersStates.Running)
        {
            return _testContainer.StartAsync();
        }

        return Task.CompletedTask;
    }

    [Test]
    public void Migrate_creates_schema_history_when_not_initialized()
    {
        var connection = _testContainer.DbConnection;

        var configuration = new Configuration(url: connection.ConnectionString);

        var migrationSeeker = Substitute.For<IMigrationSeeker>();
        var migrationApplicator = Substitute.For<IMigrationsApplicator>();

        var database = DatabaseResolver.Create(
            new NullLoggerFactory(),
            connection,
            new DbExecutor(connection),
            configuration,
            new MemoryCache(new MemoryCacheOptions())
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

        database.HasMigrationTable().Should().BeTrue();
        database.GetSchemaHistory().Should().BeEquivalentTo(Array.Empty<AppliedMigration>());
    }

    [Test]
    public void Migrate_applies_pending_migrations()
    {
        var connection = _testContainer.DbConnection;

        var configuration = new Configuration(
            url: connection.ConnectionString,
            installedBy: "SierraSam"
        );

        var database = DatabaseResolver.Create(
            new NullLoggerFactory(),
            connection,
            new DbExecutor(connection),
            configuration,
            new MemoryCache(new MemoryCacheOptions())
        );

        var migrationSeeker = Substitute.For<IMigrationSeeker>();

        var pendingMigrations = new PendingMigration[]
        {
            new("1", "Add table foo", Versioned, "CREATE TABLE foo (id INT);", "V1__Add_table_foo.sql"),
            new(null, "Write into foo", Repeatable, "INSERT INTO foo VALUES (1);", "R1__Write_into_foo.sql"),
        };

        migrationSeeker.Find().Returns(pendingMigrations);

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

        database.HasMigrationTable().Should().BeTrue();
        database.GetSchemaHistory().Should().BeEquivalentTo(new AppliedMigration[]
            {
                new(1,
                    pendingMigrations[0].Version,
                    pendingMigrations[0].Description,
                    "SQL",
                    pendingMigrations[0].FileName,
                    pendingMigrations[0].Checksum,
                    "SierraSam",
                    DateTime.MinValue.ToUniversalTime(),
                    default,
                    true),
                new(2,
                    pendingMigrations[1].Version,
                    pendingMigrations[1].Description,
                    "SQL",
                    pendingMigrations[1].FileName,
                    pendingMigrations[1].Checksum,
                    "SierraSam",
                    DateTime.MinValue.ToUniversalTime(),
                    default,
                    true)
            },
            options => options
                .Excluding(migration => migration.ExecutionTime)
                .Excluding(migration => migration.InstalledOn)
        );
    }

    [Test]
    public void Migrate_updates_schema_history_for_altered_repeatable_migration()
    {
        var connection = _testContainer.DbConnection;

        var configuration = new Configuration(
            url: connection.ConnectionString,
            installedBy: "SierraSam"
        );

        var database = DatabaseResolver.Create(
            new NullLoggerFactory(),
            connection,
            new DbExecutor(connection),
            configuration,
            new MemoryCache(new MemoryCacheOptions())
        );

        if (!database.HasMigrationTable()) database.CreateSchemaHistory();

        database.InsertSchemaHistory(
            new AppliedMigration(
                1,
                null,
                "Write into foo",
                "SQL",
                "R1__Write_into_foo.sql",
                "SELECT 1".Checksum(),
                "SierraSam",
                DateTime.UtcNow,
                0.0,
                true)
        );

        var migrationSeeker = Substitute.For<IMigrationSeeker>();

        var pendingMigrations = new PendingMigration[]
        {
            new(null, "Write into foo", Repeatable, "SELECT 2", "R1__Write_into_foo.sql"),
        };

        migrationSeeker.Find().Returns(pendingMigrations);

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

        database.GetSchemaHistory().Should().BeEquivalentTo(new AppliedMigration[]
            {
                new(1,
                    null,
                    pendingMigrations[0].Description,
                    "SQL",
                    pendingMigrations[0].FileName,
                    pendingMigrations[0].Checksum,
                    "SierraSam",
                    DateTime.MinValue.ToUniversalTime(),
                    default,
                    true)
            },
            options => options
                .Excluding(migration => migration.ExecutionTime)
                .Excluding(migration => migration.InstalledOn)
        );
    }

    [TearDown]
    public async Task ResetDatabase() => await _testContainer.Reset();

    // // TODO: How can I pull this out of this class?
    // [OneTimeTearDown]
    // public async Task OneTimeTearDown()
    // {
    //     await _container.DbConnection.DisposeAsync();
    //     await _container.StopAsync();
    // }
}