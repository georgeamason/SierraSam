using System.IO.Abstractions.TestingHelpers;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using SierraSam.Capabilities;
using SierraSam.Core;
using SierraSam.Core.Factories;
using SierraSam.Core.MigrationApplicators;
using SierraSam.Database;
using Spectre.Console.Testing;
using static SierraSam.Tests.Integration.DbContainerFactory;

namespace SierraSam.Tests.Integration.Capabilities;

internal sealed class MigrateTests
{
    private readonly ILogger<Migrate> _logger = Substitute.For<ILogger<Migrate>>();
    private readonly TestConsole _console = new ();

    private static readonly IEnumerable<IDbContainer> Containers = new IDbContainer[]
    {
        new SqlServer(),
        new Postgres(),
        new MySql()
    };

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        foreach (var container in Containers) await container.StartAsync();
    }

    [TestCaseSource(nameof(Containers))]
    public void Migrate_creates_schema_history_when_not_initialized(IDbContainer dbContainer)
    {
        var configuration = new Configuration(url: dbContainer.ConnectionString);

        using var odbcConnection = OdbcConnectionFactory.Create(_logger, configuration);

        var database = DatabaseResolver.Create(
            new LoggerFactory(),
            odbcConnection,
            new DbExecutor(odbcConnection),
            configuration,
            new MemoryCache(new MemoryCacheOptions())
        );

        var migrationSeeker = MigrationSeekerFactory.Create(
            configuration,
            new MockFileSystem()
        );

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
        database.GetSchemaHistory().Should().BeEquivalentTo(Array.Empty<AppliedMigration>());
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        foreach (var container in Containers) await container.StopAsync();
    }
}