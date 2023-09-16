using System.Data.Odbc;
using System.IO.Abstractions;
using DotNet.Testcontainers.Containers;
using FluentAssertions;
using NSubstitute;
using SierraSam.Core.Enums;

namespace SierraSam.Core.Tests.Integration;

internal sealed class MigrationApplicatorTests
{
    private const string Password = "azyxqw!df323e";

    private readonly IContainer _container = DbContainerFactory.CreateMsSqlContainer(Password);

    private readonly OdbcConnection _connection = new();

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        await _container.StartAsync();

        _connection.ConnectionString =
            "Driver={ODBC Driver 17 for SQL Server};" +
            $"Server=127.0.0.1,{_container.GetMappedPublicPort(1433)};" +
            $"UID=sa;PWD={Password};";

        await _connection.OpenAsync();
    }

    [Test]
    public void Apply_makes_expected_calls_to_odbc_executor()
    {
        var configuration = Substitute.For<IConfiguration>();

        configuration
            .DefaultSchema
            .Returns("dbo");

        var database = Substitute.For<IDatabase>();

        database
            .Connection
            .Returns(_connection);

        const string migrationSql = "CREATE TABLE dbo.Dummy (Id INT)";

        var migrationApplicator = new MigrationApplicator(database, configuration);

        var pendingMigrations = new[]
        {
            new PendingMigration(
                "1",
                "description",
                MigrationType.Versioned,
                migrationSql,
                "filename.sql")
        };

        var appliedMigrations = Array.Empty<AppliedMigration>();

        AppliedMigration appliedMigration = null!;

        database
            .WhenForAnyArgs(d => d.InsertSchemaHistory(null!, null!))
            .Do(info => appliedMigration = info.Arg<AppliedMigration>());

        var (appliedCount, executionTime) = migrationApplicator.Apply
            (pendingMigrations, appliedMigrations);

        database
            .Received(1)
            .ExecuteMigration(migrationSql, Arg.Any<OdbcTransaction>());

        database
            .ReceivedWithAnyArgs(1)
            .InsertSchemaHistory(default!, default!);

        appliedMigration.InstalledRank.Should().Be(1);
        appliedMigration.Description.Should().Be("description");
        appliedMigration.Type.Should().Be("SQL");
        appliedMigration.Script.Should().Be("filename.sql");
        appliedMigration.Checksum.Should().Be("02a983b498212d3f65c244f14de9572c");
        appliedMigration.InstalledOn.Should().BeWithin(TimeSpan.FromSeconds(1));
        appliedMigration.ExecutionTime.Should().Be(executionTime.TotalMilliseconds);
        appliedMigration.Success.Should().BeTrue();

        appliedCount.Should().Be(1);
    }

    [Test]
    public void Apply_skips_migration_when_checksum_matches()
    {
        var configuration = Substitute.For<IConfiguration>();

        configuration
            .DefaultSchema
            .Returns("dbo");

        var database = Substitute.For<IDatabase>();

        database
            .Connection
            .Returns(_connection);

        const string migrationSql = "CREATE TABLE dbo.Dummy (Id INT)";

        var migrationApplicator = new MigrationApplicator(database, configuration);

        var pendingMigrations = new[]
        {
            new PendingMigration(
                null,
                "description",
                MigrationType.Repeatable,
                migrationSql,
                string.Empty)
        };

        var appliedMigrations = new[]
        {
            new AppliedMigration(
                1,
                null,
                string.Empty,
                string.Empty,
                string.Empty,
                "02a983b498212d3f65c244f14de9572c",
                string.Empty,
                DateTime.UtcNow,
                TimeSpan.Zero.TotalMilliseconds,
                true)
        };

        var (appliedCount, executionTime) = migrationApplicator.Apply
            (pendingMigrations, appliedMigrations);

        database
            .DidNotReceiveWithAnyArgs()
            .ExecuteMigration(null!, null!);

        database
            .DidNotReceiveWithAnyArgs()
            .InsertSchemaHistory(null!, null!);

        appliedCount.Should().Be(0);
    }
}