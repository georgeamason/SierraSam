using System.Collections;
using System.Data.Odbc;
using DotNet.Testcontainers.Containers;
using FluentAssertions;
using SierraSam.Capabilities;
using SierraSam.Core;
using SierraSam.Core.Enums;
using SierraSam.Core.MigrationSeekers;
using Spectre.Console;

namespace SierraSam.Tests.Integration.Capabilities;

[TestFixture]
internal sealed class MigrateTests
{
    private readonly ILogger<Migrate> _logger = Substitute.For<ILogger<Migrate>>();

    private const string Password = "yourStrong(!)Password";

    private static IEnumerable Database_containers()
    {
        yield return new TestCaseData
            (DbContainerFactory.CreateMsSqlContainer(Password),
             "Driver={{ODBC Driver 17 for SQL Server}};" +
             "Server=127.0.0.1,{0};" +
             "UID=sa;" +
             $"PWD={Password};",
             1433,
             "dbo")
            .SetName("SQL Server");

        yield return new TestCaseData
            (DbContainerFactory.CreatePostgresContainer(Password),
             "Driver={{PostgreSQL UNICODE}};" +
             "Server=127.0.0.1;" +
             "Port={0};" +
             "Uid=sa;" +
             $"Pwd={Password};",
             5432,
             "public")
            .SetName("PostgreSQL");
    }

    [TestCaseSource(nameof(Database_containers))]
    public async Task Migrate_calls_apply_with_correct_args(
        IContainer container,
        string connectionString,
        int containerPort,
        string defaultSchema)
    {
        await container.StartAsync();

        await using var odbcConnection = new OdbcConnection(
            string.Format(connectionString, container.GetMappedPublicPort(containerPort))
        );

        const string sql = "CREATE TABLE Test(" +
                           "\"ID\" int PRIMARY KEY NOT NULL," +
                           "\"Description\" varchar(255) NOT NULL)";

        var configuration = Substitute.For<IConfiguration>();

        configuration.Url.Returns(connectionString);
        configuration.DefaultSchema.Returns(defaultSchema);
        configuration.SchemaTable.Returns("SchemaHistory");
        configuration.MigrationPrefix.Returns("V");
        configuration.MigrationSeparator.Returns("__");
        configuration.MigrationSuffixes.Returns(new []{ ".sql" });
        configuration.InstalledBy.Returns(string.Empty);

        var database = Substitute.For<IDatabase>();

        database
            .Connection
            .Returns(odbcConnection);

        database
            .HasMigrationTable
            .Returns(false);

        database
            .GetSchemaHistory(configuration.DefaultSchema, configuration.SchemaTable)
            .Returns(Array.Empty<AppliedMigration>());

        var migrationSeeker = Substitute.For<IMigrationSeeker>();

        var pendingMigrations = new[]
        {
            new PendingMigration(
                "1",
                "Test",
                MigrationType.Versioned,
                sql,
                "V1__Test.sql")
        };

        migrationSeeker
            .Find()
            .Returns(pendingMigrations);

        var migrationApplicator = Substitute.For<IMigrationsApplicator>();

        IReadOnlyCollection<PendingMigration> arg1 = null!;

        migrationApplicator
            .WhenForAnyArgs(applicator => applicator.Apply(null!))
            .Do(info =>
            {
                arg1 = info.Arg<IReadOnlyCollection<PendingMigration>>();
            });

        var console = Substitute.For<IAnsiConsole>();

        var sut = new Migrate(
            _logger,
            database,
            configuration,
            migrationSeeker,
            migrationApplicator,
            console);

        sut.Run(Array.Empty<string>());

        arg1.Should().BeEquivalentTo(pendingMigrations);

        await container.StopAsync();
    }
}