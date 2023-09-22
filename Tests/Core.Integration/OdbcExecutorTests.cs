using System.Data.Odbc;
using DotNet.Testcontainers.Containers;
using FluentAssertions;
using SierraSam.Core.Exceptions;

namespace SierraSam.Core.Tests.Integration;

internal sealed class OdbcExecutorTests
{
    private const string Password = "yourStrong(!)Password";

    private readonly IContainer _container;

    private readonly OdbcConnection _connection;

    public OdbcExecutorTests()
    {
        _container = DbContainerFactory.CreateMsSqlContainer(Password);

        _connection = new OdbcConnection();
    }

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
        using var command = new OdbcCommand
            ("DROP TABLE IF EXISTS dbo.Dummy", _connection);

        command.ExecuteNonQuery();
    }

    [Test]
    public void ExecuteReader_returns_empty_collection_when_no_rows()
    {
        var odbcExecutor = new OdbcExecutor(_connection);

        odbcExecutor.ExecuteNonQuery("CREATE TABLE dbo.Dummy (Id INT)");

        odbcExecutor
            .ExecuteReader<string>("SELECT Id FROM dbo.Dummy", reader => reader.GetString(0))
            .Should()
            .BeSameAs(Array.Empty<string>());
    }

    [Test]
    public void ExecuteReader_returns_collection_of_rows()
    {
        var odbcExecutor = new OdbcExecutor(_connection);

        odbcExecutor.ExecuteNonQuery("CREATE TABLE dbo.Dummy (Id INT)");

        odbcExecutor.ExecuteNonQuery("INSERT INTO dbo.Dummy VALUES (1)");
        odbcExecutor.ExecuteNonQuery("INSERT INTO dbo.Dummy VALUES (2)");
        odbcExecutor.ExecuteNonQuery("INSERT INTO dbo.Dummy VALUES (3)");

        odbcExecutor
            .ExecuteReader<string>("SELECT Id FROM dbo.Dummy", reader => reader.GetString(0))
            .Should()
            .BeEquivalentTo("1", "2", "3");
    }

    [Test]
    public void ExecuteReader_throws_when_sql_is_invalid()
    {
        var odbcExecutor = new OdbcExecutor(_connection);

        odbcExecutor
            .Invoking(x => x.ExecuteReader<string>("SELECT Id FROM dbo.Dummy", reader => reader.GetString(0)))
            .Should()
            .Throw<OdbcExecutorException>()
            .WithMessage("Failed to execute SQL statement: 'SELECT Id FROM dbo.Dummy'");
    }

    [Test]
    public void ExecuteNonQuery_executes_sql_statement()
    {
        var odbcExecutor = new OdbcExecutor(_connection);

        odbcExecutor.ExecuteNonQuery("CREATE TABLE dbo.Dummy (Id INT)");

        odbcExecutor
            .ExecuteReader<string>("SELECT Id FROM dbo.Dummy", reader => reader.GetString(0))
            .Should()
            .BeSameAs(Array.Empty<string>());
    }

    [Test]
    public void ExecuteNonQuery_throws_when_sql_is_invalid()
    {
        var odbcExecutor = new OdbcExecutor(_connection);

        odbcExecutor
            .Invoking(x => x.ExecuteNonQuery("SELECT Id FROM dbo.Dummy"))
            .Should()
            .Throw<OdbcExecutorException>()
            .WithMessage("Failed to execute SQL statement: 'SELECT Id FROM dbo.Dummy'");
    }

    [OneTimeTearDown]
    public async Task Dispose()
    {
        await _connection.DisposeAsync();
    }
}