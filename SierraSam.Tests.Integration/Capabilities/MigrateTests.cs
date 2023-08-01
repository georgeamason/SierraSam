using System.Collections;
using System.Data.Odbc;
using System.IO.Abstractions.TestingHelpers;
using System.Text;
using DotNet.Testcontainers.Containers;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using SierraSam.Capabilities;
using SierraSam.Core;
using SierraSam.Core.Extensions;
using SierraSam.Core.Factories;
using SierraSam.Core.MigrationSeekers;
using SierraSam.Database;


namespace SierraSam.Tests.Integration.Capabilities;

[TestFixture]
internal sealed class MigrateTests
{
    private readonly ILogger<Migrate> _logger;

    private const string Password = "yourStrong(!)Password";

    public MigrateTests()
    {
        _logger = Substitute.For<ILogger<Migrate>>();
    }

    private static IEnumerable Database_containers()
    {
        yield return new TestCaseData
            (DbContainerFactory.CreateMsSqlContainer(Password),
             $"Driver={{ODBC Driver 17 for SQL Server}};Server=127.0.0.1,1433;UID=sa;PWD={Password};",
             "dbo")
            .SetName("SQL Server");

        yield return new TestCaseData
            (DbContainerFactory.CreatePostgresContainer(Password),
             $"Driver={{PostgreSQL UNICODE}};Server=127.0.0.1;Port=5432;Uid=sa;Pwd={Password};",
             "public")
            .SetName("PostgreSQL");
    }

    [TestCaseSource(nameof(Database_containers))]
    public async Task Migrate_updates_database_correctly
        (IContainer container, string connectionString, string defaultSchema)
    {
        await container.StartAsync();

        await using var odbcConnection = new OdbcConnection(connectionString);

        var mockFileSystem = new MockFileSystem();

        var contents = Encoding.UTF8.GetBytes
            ("CREATE TABLE Test(" +
             "\"ID\" int PRIMARY KEY NOT NULL," +
             "\"Description\" varchar(255) NOT NULL)");

        mockFileSystem.AddDirectory("db/migration");

        mockFileSystem.AddFile
            ("db/migration/V1__Test.sql",
             new MockFileData(contents));

        var configuration = new Configuration
            (url: connectionString,
             defaultSchema: defaultSchema);

        var database = DatabaseFactory.Create
            (odbcConnection, configuration);

        var migrationSeeker = Substitute.For<IMigrationSeeker>();

        migrationSeeker
            .Find()
            .Returns(new[] { "db/migration/V1__Test.sql" });

        var migrationApplicator = new MigrationApplicator
            (database, mockFileSystem, configuration);

        var migrate = new Migrate
            (_logger,
             database,
             configuration,
             mockFileSystem,
             migrationSeeker,
             migrationApplicator);

        var args = Array.Empty<string>();

        migrate.Run(args);

        var migrations = database
            .GetSchemaHistory(configuration.DefaultSchema, configuration.SchemaTable)
            .ToArray();

        migrations.Should().HaveCount(1);

        migrations[0].Version.Should().Be("1");
        migrations[0].Description.Should().Be("Test");
        migrations[0].Type.Should().Be("SQL");
        migrations[0].Script.Should().Be("V1__Test.sql");
        migrations[0].Checksum.Should().Be("72e60a278ed8d3655565a63940a34c2c");
        migrations[0].InstalledBy.Should().Be(string.Empty);
        migrations[0].InstalledOn.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        migrations[0].Success.Should().BeTrue();

        database.HasTable(configuration.SchemaTable).Should().BeTrue();

        await container.StopAsync();
    }
}