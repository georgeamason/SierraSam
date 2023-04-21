using System.Collections;
using System.Data;
using System.Data.Odbc;
using System.IO.Abstractions.TestingHelpers;
using System.Text;
using DotNet.Testcontainers.Containers;
using FluentAssertions;
using SierraSam.Capabilities;
using SierraSam.Core;
using SierraSam.Core.Extensions;
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
             "dbo");

        yield return new TestCaseData
            (DbContainerFactory.CreatePostgresContainer(Password),
             $"Driver={{PostgreSQL UNICODE}};Server=127.0.0.1;Port=5432;Uid=sa;Pwd={Password};",
             "public");
    }

    [TestCaseSource(nameof(Database_containers))]
    public async Task Migrate_updates_database_correctly
        (IContainer container, string connectionString, string defaultSchema)
    {
        await container.StartAsync();

        var odbcConnection = new OdbcConnection(connectionString);

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
            { DefaultSchema = defaultSchema };

        var migrate = new Migrate
            (_logger,
             DatabaseFactory.Create(odbcConnection, configuration), 
             configuration,
             mockFileSystem);

        var args = Array.Empty<string>();

        migrate.Run(args);

        using var schemaHistory = DbQueryHandler.ExecuteSql
            (connectionString,
             $"SELECT * FROM {configuration.DefaultSchema}.{configuration.SchemaTable}");

        schemaHistory.Rows.Count.Should().Be(1);

        schemaHistory.Rows[0].GetString("version")
            .Should().Be("1");

        schemaHistory.Rows[0].GetString("description")
            .Should().Be("Test");
        
        schemaHistory.Rows[0].GetString("type")
            .Should().Be("SQL");

        schemaHistory.Rows[0].GetString("script")
            .Should().Be("V1__Test.sql");

        schemaHistory.Rows[0].GetString("checksum")
            .Should().Be("72e60a278ed8d3655565a63940a34c2c");

        schemaHistory.Rows[0].GetString("installed_by")
            .Should().Be(string.Empty);

        schemaHistory.Rows[0].GetDateTime("installed_on")
            .Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));

        schemaHistory.Rows[0].GetBoolean("success")
            .Should().BeTrue();

        using var migration = DbQueryHandler.ExecuteSql
            (connectionString,
             "SELECT * FROM \"information_schema\".\"tables\" " +
             "WHERE \"table_type\" = 'BASE TABLE' " +
             "AND LOWER(\"table_name\") = 'test'");

        migration.Rows.Count.Should().Be(1);

        await container.StopAsync();
    }
}