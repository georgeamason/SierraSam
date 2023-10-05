using System.Collections;
using System.Data.Odbc;
using DotNet.Testcontainers.Containers;
using FluentAssertions;
using NSubstitute;
using SierraSam.Core.Enums;
using SierraSam.Core.Extensions;

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

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        await _connection.CloseAsync();
        await _container.StopAsync();
        await _container.DisposeAsync();
    }

    private static IEnumerable Migrations_to_apply()
    {
        yield return new TestCaseData(
            (object)new PendingMigration[]
            {
                new(
                    "1",
                    "description",
                    MigrationType.Versioned,
                    "CREATE TABLE dbo.Dummy (Id INT)",
                    "filename.sql")
            }).SetName("Single versioned migration");

        yield return new TestCaseData(
            (object)new[]
            {
                new PendingMigration(
                    "1",
                    "description",
                    MigrationType.Versioned,
                    "CREATE TABLE dbo.Dummy (Id INT)",
                    "filename.sql"),
                new PendingMigration(
                    "2",
                    "someDescription",
                    MigrationType.Versioned,
                    "CREATE TABLE dbo.Dummy2 (Id INT)",
                    "filename2.sql"),
            }).SetName("Multiple versioned migrations");

        yield return new TestCaseData(
            (object)new []
        {
            new PendingMigration(
                null,
                "description",
                MigrationType.Repeatable,
                "CREATE TABLE dbo.Dummy (Id INT)",
                "filename.sql")
        }).SetName("Single repeatable migration");
    }

    [TestCaseSource(nameof(Migrations_to_apply))]
    public void Apply_makes_expected_database_calls(PendingMigration[] pendingMigrations)
    {
        var configuration = Substitute.For<IConfiguration>();

        configuration
            .DefaultSchema
            .Returns("dbo");

        var database = Substitute.For<IDatabase>();

        database
            .Connection
            .Returns(_connection);

        var executionTime = TimeSpan.FromSeconds(1);

        database
            .ExecuteMigration(Arg.Any<string>(), Arg.Any<OdbcTransaction>())
            .Returns(executionTime);

        var sut = new MigrationApplicator(database, configuration);

        var (appliedCount, totalExecutionTime) = sut.Apply(
            pendingMigrations,
            Array.Empty<AppliedMigration>()
        );

        var installedRank = 1;
        foreach (var pendingMigration in pendingMigrations)
        {
            database
                .Received()
                .ExecuteMigration(pendingMigration.Sql, Arg.Any<OdbcTransaction>());

            database
                .Received()
                .InsertSchemaHistory(Arg.Is<AppliedMigration>(
                        // ReSharper disable once AccessToModifiedClosure
                        // ReSharper disable once CompareOfFloatsByEqualityOperator
                        m => m.InstalledRank == installedRank &&
                             m.Version == pendingMigration.Version &&
                             m.Description == pendingMigration.Description &&
                             m.Type == "SQL" &&
                             m.Script == pendingMigration.FileName &&
                             m.Checksum == pendingMigration.Checksum &&
                             m.ExecutionTime == executionTime.TotalMilliseconds &&
                             m.Success == true
                    ),
                    Arg.Any<OdbcTransaction>());

            installedRank++;
        }

        appliedCount.Should().Be(pendingMigrations.Length);
        totalExecutionTime.Should().Be(TimeSpan.FromSeconds(pendingMigrations.Length));
    }

    [Test]
    public void Apply_skips_repeatable_migration_when_checksum_matches()
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

        var sut = new MigrationApplicator(database, configuration);

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
                migrationSql.Checksum(),
                string.Empty,
                DateTime.UtcNow,
                TimeSpan.Zero.TotalMilliseconds,
                true)
        };

        var (appliedCount, _) = sut.Apply(pendingMigrations, appliedMigrations);

        database
            .DidNotReceiveWithAnyArgs()
            .ExecuteMigration(default!);

        database
            .DidNotReceiveWithAnyArgs()
            .InsertSchemaHistory(default!);

        appliedCount.Should().Be(0);
    }

    [Test]
    public void Apply_updates_repeatable_migration_when_script_has_changed()
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

        var sut = new MigrationApplicator(database, configuration);

        var pendingMigrations = new[]
        {
            new PendingMigration(
                null,
                "description",
                MigrationType.Repeatable,
                migrationSql,
                "filename.sql")
        };

        var appliedMigrations = new[]
        {
            new AppliedMigration(
                1,
                null,
                string.Empty,
                string.Empty,
                "filename.sql",
                "someOtherChecksum",
                string.Empty,
                DateTime.UtcNow,
                TimeSpan.Zero.TotalMilliseconds,
                true)
        };

        var (appliedCount, _) = sut.Apply(pendingMigrations, appliedMigrations);

        database
            .Received(1)
            .UpdateSchemaHistory(
                Arg.Is<AppliedMigration>(
                    m => m.Checksum == migrationSql.Checksum() &&
                         m.Script == "filename.sql"
                ),
                Arg.Any<OdbcTransaction>()
            );

        database
            .Received(1)
            .ExecuteMigration(migrationSql, Arg.Any<OdbcTransaction>());

        appliedCount.Should().Be(0);
    }
}