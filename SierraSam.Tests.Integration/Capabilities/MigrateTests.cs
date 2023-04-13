using System.Data;
using System.Data.Odbc;
using System.IO.Abstractions.TestingHelpers;
using System.Text;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using SierraSam.Capabilities;
using SierraSam.Core;
using SierraSam.Core.Factories;


namespace SierraSam.Tests.Integration.Capabilities;

[TestFixture]
internal sealed class MigrateTests
{
    private readonly IContainer _container;

    private readonly ILogger<Migrate> _logger;

    private const string Password = "yourStrong(!)Password";

    private const int PortBinding = 1433;

    private const string DatabaseName = "TestDB";

    public MigrateTests()
    {
        _container = new ContainerBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
            .WithPortBinding(1433, PortBinding)
            .WithEnvironment("ACCEPT_EULA", "Y")
            .WithEnvironment("MSSQL_SA_PASSWORD", Password)
            .WithWaitStrategy
                (Wait
                    .ForUnixContainer()
                    .UntilCommandIsCompleted
                        ("/opt/mssql-tools/bin/sqlcmd",
                         "-S", $"localhost,{PortBinding}",
                         "-U", "sa",
                         "-P", Password,
                         "-Q", $"CREATE DATABASE {DatabaseName}"))
            .Build();

        _logger = Substitute.For<ILogger<Migrate>>();
    }

    [OneTimeSetUp]
    public async Task SetUp()
    {
        await _container.StartAsync();
    }

    [Test]
    public void Migrate_updates_database_correctly()
    {
        var connectionString = $"Driver={{ODBC Driver 17 for SQL Server}};" +
                               $"Server=localhost,{PortBinding};" +
                               $"Database={DatabaseName};" +
                               $"UID=sa;" +
                               $"PWD={Password};";
        
        var odbcConnection = new OdbcConnection(connectionString);

        var mockFileSystem = new MockFileSystem();

        var contents = Encoding.UTF8.GetBytes
            ("CREATE TABLE Test(" +
             "ID int PRIMARY KEY NOT NULL," +
             "Description nvarchar(255) NOT NULL)");

        mockFileSystem.AddDirectory("db/migration");

        mockFileSystem.AddFile
            ("db/migration/V1__Test.sql",
             new MockFileData(contents));

        var configuration = new Configuration();

        var migrate = new Migrate
            (_logger,
             odbcConnection,
             configuration,
             mockFileSystem);

        var args = Array.Empty<string>();

        migrate.Run(args);

        using var schemaHistory = DbQueryHandler.ExecuteSql
            (connectionString,
             "SELECT * FROM dbo.flyway_schema_history");

        schemaHistory.Should().NotBeNull();

        schemaHistory!.Rows.Count.Should().Be(1);

        schemaHistory.Rows[0].Field<string>("version")
            .Should().Be("1");

        schemaHistory.Rows[0].Field<string>("description")
            .Should().Be("Test");

        schemaHistory.Rows[0].Field<string>("type")
            .Should().Be("SQL");

        schemaHistory.Rows[0].Field<string>("script")
            .Should().Be("V1__Test.sql");

        schemaHistory.Rows[0].Field<string>("checksum")
            .Should().Be("3ce34a47e53303ff1abdea9e9162bac3");

        schemaHistory.Rows[0].Field<string>("installed_by")
            .Should().Be(string.Empty);

        schemaHistory.Rows[0].Field<DateTime>("installed_on")
            .Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));

        schemaHistory.Rows[0].Field<bool>("success")
            .Should().Be(true);

        using var migration = DbQueryHandler.ExecuteSql
            (connectionString,
             "SELECT * FROM INFORMATION_SCHEMA.TABLES " +
             "WHERE TABLE_TYPE = 'BASE TABLE' " +
             "AND TABLE_NAME = 'Test'");

        migration.Should().NotBeNull();
    }

    [OneTimeTearDown]
    public async Task TearDown()
    {
        await _container.StopAsync();
    }
}