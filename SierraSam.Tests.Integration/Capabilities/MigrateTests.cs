using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using SierraSam.Capabilities;
using SierraSam.Core;
using SierraSam.Core.Enums;
using SierraSam.Core.Extensions;
using SierraSam.Core.MigrationApplicators;
using SierraSam.Core.MigrationSeekers;
using SierraSam.Database;
using Spectre.Console.Testing;
using static SierraSam.Tests.Integration.DbContainerFactory;

namespace SierraSam.Tests.Integration.Capabilities;

[TestFixtureSource(typeof(DbContainerFactory), nameof(ContainerTestCases))]
internal sealed class MigrateTests
{
    private readonly IDbContainer _container;
    private readonly ILogger<Migrate> _logger = Substitute.For<ILogger<Migrate>>();
    private readonly TestConsole _console = new ();

    public MigrateTests(IDbContainer container) => _container = container;

    [OneTimeSetUp]
    public async Task OneTimeSetUp() => await _container.StartAsync();

    [Test]
    public void Migrate_creates_schema_history_when_not_initialized()
    {
        var connection = _container.DbConnection;

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
        var connection = _container.DbConnection;

        var configuration = new Configuration(url: connection.ConnectionString);

        var database = DatabaseResolver.Create(
            new NullLoggerFactory(),
            connection,
            new DbExecutor(connection),
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

        database.HasMigrationTable().Should().BeTrue();
        database.GetSchemaHistory().Should().BeEquivalentTo(new AppliedMigration[]
            {
                new(1,
                    "1",
                    "Add table foo",
                    "SQL",
                    string.Empty,
                    "CREATE TABLE foo (id INT);".Checksum(),
                    string.Empty,
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
    public async Task ResetDatabase() => await _container.Clean();

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        await _container.DbConnection.DisposeAsync();
        await _container.StopAsync();
    }
}