using System.Data.Odbc;
using DotNet.Testcontainers.Containers;
using FluentAssertions;
using SierraSam.Core.Exceptions;

namespace SierraSam.Core.Tests.Integration;

internal sealed class DbExecutorTests
{
    private const string Password = "yourStrong(!)Password";
    private readonly IContainer _container = DbContainerFactory.CreateMsSqlContainer(Password);
    private readonly OdbcConnection _connection = new ();

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

    [SetUp]
    public void SetUp()
    {
        using var command = new OdbcCommand(
            "DROP TABLE IF EXISTS dbo.Dummy",
            _connection
        );

        command.ExecuteNonQuery();
    }

    [Test]
    public void ExecuteReader_returns_empty_collection_when_no_rows()
    {
        var odbcExecutor = new DbExecutor(_connection);

        odbcExecutor.ExecuteNonQuery("CREATE TABLE dbo.Dummy (Id INT)");

        odbcExecutor
            .ExecuteReader(
                "SELECT Id FROM dbo.Dummy",
                reader => reader.GetInt32(0)
            )
            .Should()
            .BeEquivalentTo(Array.Empty<int>());
    }

    [Test]
    public void ExecuteReader_returns_collection_of_rows()
    {
        var odbcExecutor = new DbExecutor(_connection);

        odbcExecutor.ExecuteNonQuery("CREATE TABLE dbo.Dummy (Id INT)");
        odbcExecutor.ExecuteNonQuery("INSERT INTO dbo.Dummy (Id) VALUES (1), (2), (3)");

        odbcExecutor
            .ExecuteReader(
                "SELECT Id FROM dbo.Dummy",
                reader => reader.GetInt32(0)
            )
            .Should()
            .BeEquivalentTo(new [] {1, 2, 3});
    }

    [Test]
    public void ExecuteReader_throws_when_sql_is_invalid()
    {
        var odbcExecutor = new DbExecutor(_connection);

        odbcExecutor
            .Invoking(x => x.ExecuteReader(
                "SELECT Id FROM dbo.Dummy",
                reader => reader.GetInt32(0))
            )
            .Should()
            .Throw<OdbcExecutorException>()
            .WithMessage("Failed to execute SQL statement: 'SELECT Id FROM dbo.Dummy'");
    }

    [Test]
    public void ExecuteNonQuery_executes_sql_statement()
    {
        var odbcExecutor = new DbExecutor(_connection);

        odbcExecutor.ExecuteNonQuery("CREATE TABLE dbo.Dummy (Id INT)");

        odbcExecutor
            .ExecuteNonQuery("INSERT INTO dbo.Dummy (Id) VALUES (1)")
            .Should()
            .Be(1);
    }

    [Test]
    public void ExecuteNonQuery_utilises_transaction()
    {
        var odbcExecutor = new DbExecutor(_connection);

        odbcExecutor.ExecuteNonQuery("CREATE TABLE dbo.Dummy (Id INT)");

        using var transaction = _connection.BeginTransaction();

        odbcExecutor
            .ExecuteNonQuery("INSERT INTO dbo.Dummy (Id) VALUES (1)", transaction)
            .Should()
            .Be(1);

        transaction.Rollback();

        odbcExecutor.ExecuteScalar<int>("SELECT COUNT(*) FROM dbo.Dummy")
            .Should()
            .Be(0);
    }

    [Test]
    public void ExecuteNonQuery_throws_when_sql_is_invalid()
    {
        var odbcExecutor = new DbExecutor(_connection);

        odbcExecutor
            .Invoking(x => x.ExecuteNonQuery("SELECT Id FROM dbo.Dummy"))
            .Should()
            .Throw<OdbcExecutorException>()
            .WithMessage("Failed to execute SQL statement: 'SELECT Id FROM dbo.Dummy'");
    }

    [Test]
    public void ExecuteScalar_returns_scalar_value()
    {
        var odbcExecutor = new DbExecutor(_connection);

        odbcExecutor.ExecuteNonQuery("CREATE TABLE dbo.Dummy (Id INT)");
        odbcExecutor.ExecuteNonQuery("INSERT INTO dbo.Dummy (Id) VALUES (1)");

        odbcExecutor
            .ExecuteScalar<int>("SELECT COUNT(*) FROM dbo.Dummy")
            .Should()
            .Be(1);
    }

    [Test]
    public void ExecuteScalar_throws_for_incorrect_type()
    {
        var odbcExecutor = new DbExecutor(_connection);

        odbcExecutor.ExecuteNonQuery("CREATE TABLE dbo.Dummy (Id INT)");
        odbcExecutor.ExecuteNonQuery("INSERT INTO dbo.Dummy (Id) VALUES (1)");

        odbcExecutor
            .Invoking(x => x.ExecuteScalar<Guid>("SELECT COUNT(*) FROM dbo.Dummy"))
            .Should()
            .Throw<ArgumentException>()
            .WithMessage("Database return was not of type 'System.Guid' *");
    }

    [Test]
    public void ExecuteScalar_returns_default_for_db_null()
    {
        var odbcExecutor = new DbExecutor(_connection);

        odbcExecutor.ExecuteNonQuery("CREATE TABLE dbo.Dummy (Id INT)");

        odbcExecutor
            .ExecuteScalar<int>("SELECT COUNT(*) FROM dbo.Dummy")
            .Should()
            .Be(0);
    }

    [Test]
    public void ExecuteScalar_throws_when_sql_is_invalid()
    {
        var odbcExecutor = new DbExecutor(_connection);

        odbcExecutor
            .Invoking(x => x.ExecuteScalar<Guid>("SELECT COUNT(*) FROM dbo.Dummy"))
            .Should()
            .Throw<OdbcExecutorException>()
            .WithMessage("Failed to execute SQL statement: 'SELECT COUNT(*) FROM dbo.Dummy'");
    }

    [OneTimeTearDown]
    public async Task Dispose()
    {
        await _connection.DisposeAsync();
        await _container.DisposeAsync();
    }
}