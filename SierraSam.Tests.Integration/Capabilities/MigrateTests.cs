using System.Collections;
using System.Data.Odbc;
using System.IO.Abstractions.TestingHelpers;
using System.Text;
using DotNet.Testcontainers.Containers;
using FluentAssertions;
using SierraSam.Capabilities;
using SierraSam.Core;
using SierraSam.Core.Enums;
using SierraSam.Core.MigrationSeekers;
using SierraSam.Database;


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
    public async Task Migrate_updates_database_correctly
        (IContainer container, string connectionString, int containerPort, string defaultSchema)
    {
        await container.StartAsync();

        await using var odbcConnection = new OdbcConnection
            (string.Format(connectionString, container.GetMappedPublicPort(containerPort)));

        var mockFileSystem = new MockFileSystem();

        var contents = Encoding.UTF8.GetBytes
            ("CREATE TABLE Test(" +
             "\"ID\" int PRIMARY KEY NOT NULL," +
             "\"Description\" varchar(255) NOT NULL)");

        mockFileSystem.AddDirectory("db/migration");

        mockFileSystem.AddFile
            ("db/migration/V1__Test.sql",
             new MockFileData(contents));

        var configuration = Substitute.For<IConfiguration>();

        configuration.Url.Returns(connectionString);
        configuration.DefaultSchema.Returns(defaultSchema);
        configuration.SchemaTable.Returns("SchemaHistory");
        configuration.MigrationPrefix.Returns("V");
        configuration.MigrationSeparator.Returns("__");
        configuration.MigrationSuffixes.Returns(new []{ ".sql" });
        configuration.InstalledBy.Returns(string.Empty);

        var database = DatabaseResolver.Create
            (odbcConnection, configuration);

        var migrationSeeker = Substitute.For<IMigrationSeeker>();

        migrationSeeker
            .Find()
            .Returns(new[]
            {
                new PendingMigration
                    ("1",
                     "Test",
                     MigrationType.Versioned,
                     "72e60a278ed8d3655565a63940a34c2c",
                     "db/migration/V1__Test.sql",
                     "V1__Test.sql")
            });

        var migrationApplicator = new MigrationApplicator
            (database, mockFileSystem, configuration);

        var migrate = new Migrate
            (_logger,
             database,
             configuration,
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