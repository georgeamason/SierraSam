using System.Collections;
using System.IO.Abstractions.TestingHelpers;
using DotNet.Testcontainers.Containers;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using SierraSam.Capabilities;
using SierraSam.Core;
using SierraSam.Core.Factories;
using SierraSam.Core.MigrationApplicators;
using SierraSam.Database;
using Spectre.Console.Testing;

namespace SierraSam.Tests.Integration.Capabilities;

internal sealed class MigrateTests
{
    private readonly ILogger<Migrate> _logger = Substitute.For<ILogger<Migrate>>();
    private readonly TestConsole _console = new ();

    private const string Password = "yourStrong(!)Password";

    private static IEnumerable Database_containers()
    {
        yield return new TestCaseData(
                DbContainerFactory.CreateMsSqlContainer(Password),
                $"Driver={{{{ODBC Driver 17 for SQL Server}}}};Server=127.0.0.1,{{0}};UID=sa;PWD={Password};",
                1433
            )
            .SetName("SQL Server");

        yield return new TestCaseData(
                DbContainerFactory.CreatePostgresContainer(Password),
                $"Driver={{{{PostgreSQL UNICODE}}}};Server=127.0.0.1;Port={{0}};Uid=sa;Pwd={Password};",
                5432
            )
            .SetName("PostgreSQL");
    }
    [TestCaseSource(nameof(Database_containers))]
    public async Task Migrate_creates_schema_history_when_not_initialized(
        IContainer container,
        string connectionString,
        int containerPort
    )
    {
        await container.StartAsync();

        var configuration = new Configuration(
            url: string.Format(connectionString, container.GetMappedPublicPort(containerPort))
        );

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

        await container.StopAsync();
    }
}